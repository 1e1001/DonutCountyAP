use std::fs::File;
use std::io::BufReader;
use std::path::Path;

use quick_xml::escape::resolve_xml_entity;
use quick_xml::events::{BytesStart, Event};
use quick_xml::name::QName;

pub trait FromCell {
	fn from_cell(text: &str) -> Self;
}

impl FromCell for usize {
	fn from_cell(text: &str) -> Self {
		text.parse().unwrap_or_else(|err| panic!("invalid number {text:?}: {err:?}"))
	}
}

impl FromCell for i32 {
	fn from_cell(text: &str) -> Self {
		text.parse().unwrap_or_else(|err| panic!("invalid number {text:?}: {err:?}"))
	}
}

impl FromCell for String {
	fn from_cell(text: &str) -> Self { text.to_owned() }
}

impl<T: FromCell> FromCell for Option<T> {
	fn from_cell(text: &str) -> Self { (!text.is_empty()).then(|| T::from_cell(text)) }
}

impl<T: FromCell> FromCell for Vec<T> {
	fn from_cell(text: &str) -> Self {
		if text.is_empty() { Vec::new() } else { text.split(';').map(T::from_cell).collect() }
	}
}

pub struct SheetReader {
	reader: quick_xml::Reader<BufReader<File>>,
	buf: Vec<u8>,
}

impl SheetReader {
	fn new(path: impl AsRef<Path>) -> Self {
		Self { reader: quick_xml::Reader::from_file(path).expect("opening xml"), buf: Vec::new() }
	}

	fn finish(mut self) {
		loop {
			match self.read_event() {
				Event::Eof => break,
				Event::Text(_) => {},
				event => panic!("invalid eof event {event:?}"),
			}
			self.buf.clear();
		}
		self.buf.clear();
	}

	fn read_event(&mut self) -> Event<'_> {
		self.reader.read_event_into(&mut self.buf).expect("reading xml")
	}

	fn close(&mut self, parent: QName) {
		self.reader.read_to_end_into(parent, &mut self.buf).unwrap();
		self.buf.clear();
	}

	fn open(&mut self, name: QName) -> BytesStart<'static> {
		let result = loop {
			match self.read_event() {
				Event::Text(_) | Event::Empty(_) | Event::Decl(_) => {},
				Event::Start(tag) if tag.name() == name => {
					let tag = tag.into_owned();
					break tag;
				},
				Event::Start(tag) => {
					let end = tag.to_end().into_owned();
					self.close(end.name());
					// skip clearing twice
					continue;
				},
				event => panic!("invalid open event {event:?}"),
			}
			self.buf.clear();
		};
		self.buf.clear();
		result
	}

	fn close_with_text(&mut self, parent: QName) -> String {
		let mut result = String::new();
		loop {
			match self.read_event() {
				Event::End(tag) if tag.name() == parent => {
					break;
				},
				Event::Text(text) => result.push_str(&text.into_inner()),
				Event::GeneralRef(text) => {
					result.push_str(resolve_xml_entity(&text).expect("xml entity"))
				},
				event => panic!("invalid close_with_text event {event:?}"),
			}
			self.buf.clear();
		}
		self.buf.clear();
		result
	}

	fn open_or_close(&mut self, name: QName, parent: QName) -> Option<BytesStart<'static>> {
		let result = loop {
			match self.read_event() {
				Event::End(tag) if tag.name() == parent => {
					break None;
				},
				Event::Text(_) | Event::Empty(_) => {},
				Event::Start(tag) if tag.name() == name => {
					let tag = tag.into_owned();
					break Some(tag);
				},
				Event::Start(tag) => {
					let end = tag.to_end().into_owned();
					self.close(end.name());
					// skip clearing twice
					continue;
				},
				event => panic!("invalid open_or_close event {event:?}"),
			}
			self.buf.clear();
		};
		self.buf.clear();
		result
	}

	fn read_table_header(&mut self) -> Vec<String> {
		let mut names = Vec::new();
		self.open(QName("Row"));
		while let Some(cell) = self.open_or_close(QName("Cell"), QName("Row")) {
			assert_eq!(cell.try_get_attribute("ss:Index").unwrap(), None, "gap in header");
			self.open(QName("Data"));
			//let data = self.open(QName("Data"));
			//assert_eq!(
			//	data.try_get_attribute("ss:Type").unwrap().map(|attr| attr.value).as_deref(),
			//	Some("String"),
			//	"non-string header"
			//);
			names.push(self.close_with_text(QName("Data")));
			self.close(QName("Cell"));
		}
		names
	}

	pub fn sheet<const N: usize, F: FnMut([&str; N])>(&mut self, (columns, mut f): ([&str; N], F)) {
		self.open(QName("Table"));
		let header = self.read_table_header();
		assert_eq!(header, columns, "mismatched headers");
		while let Some(_) = self.open_or_close(QName("Row"), QName("Table")) {
			let mut row = [const { String::new() }; N];
			let mut index = 0;
			while let Some(cell) = self.open_or_close(QName("Cell"), QName("Row")) {
				if let Some(new_index) = cell.try_get_attribute("ss:Index").unwrap() {
					index = new_index.value.parse::<usize>().unwrap() - 1;
				}
				//let data = self.open(QName("Data"))
				//let data_type = data.try_get_attribute("ss:Type").unwrap().expect("no data type");
				//let text = self.close_with_text(QName("Data"));
				//row[index] = match &*data_type.value {
				//	"String" => CellData::String(text),
				//	"Number" => CellData::Number(text.parse().expect("number")),
				//	data_type => panic!("bad data type {data_type:?}"),
				//};
				self.open(QName("Data"));
				row[index] = self.close_with_text(QName("Data"));
				index += 1;
				self.close(QName("Cell"));
			}
			f(row.each_ref().map(String::as_str));
		}
	}

	pub fn read_book(path: impl AsRef<Path>, mut f: impl FnMut(&mut Self, &str)) {
		let mut this = Self::new(path);
		this.open(QName("Workbook"));
		while let Some(worksheet) = this.open_or_close(QName("Worksheet"), QName("Workbook")) {
			f(
				&mut this,
				&worksheet.try_get_attribute("ss:Name").unwrap().expect("worksheet name").value,
			);
			this.close(QName("Worksheet"));
		}
		this.finish();
	}

	pub fn read_csproj(path: impl AsRef<Path>) -> String {
		let mut this = Self::new(path);
		this.open(QName("Project"));
		this.open(QName("PropertyGroup"));
		this.open(QName("Version"));
		let text = this.close_with_text(QName("Version"));
		this.close(QName("PropertyGroup"));
		this.close(QName("Project"));
		this.finish();
		text
	}
}

pub const fn clean_header(text: &str) -> &str {
	let bytes = text.as_bytes();
	let [b'r', b'#', rest @ ..] = bytes else { return text };
	let Ok(text) = str::from_utf8(rest) else {
		unreachable!();
	};
	text
}

#[macro_export]
macro_rules! worksheet {
	(@count $id:ident) => { 1 };
	(@count $($id:ident)*) => { ($($crate::worksheet!(@count $id)+)* 0) };
	(|$($((mut $($mut:lifetime)?))? $id:ident: $ty:ty),* $(,)?| $body:block) => {
		([$($crate::sheet::clean_header(stringify!($id)),)*], |[$($id,)*]: [&str; $crate::worksheet!(@count $($id)*)]| {
			$(let $($($mut)? mut)? $id = <$ty>::from_cell($id);)*
			$body
		})
	}
}
