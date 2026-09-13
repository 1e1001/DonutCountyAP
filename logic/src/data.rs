use std::collections::{BTreeMap, BTreeSet};
use std::mem::replace;
use std::{fmt, ops};

use hashbrown::{HashMap, HashSet};
use serde::{Deserialize, Deserializer, Serialize};
use serde_repr::Serialize_repr;

use crate::sheet::FromCell;

#[derive(Clone, Debug, PartialEq, Eq, Hash)]
pub enum Rule {
	Hole(String),
	Catapult(String),
	Trash(String),
	Level(usize),
	SaltPepper(bool),
	SnakeDangerAll,
	Texting,
	Glitches,
	And(Vec<Rule>),
	Or(Vec<Rule>),
	AndTrash(Vec<Rule>),
	True,
}
impl Rule {
	fn token(token: ssexp::Token) -> Self {
		match token {
			ssexp::Token::Symbol(text) if text.starts_with("Hole") => Self::Hole(text.into()),
			ssexp::Token::Symbol(text) if text.starts_with("Catapult") => {
				Self::Catapult(text.into())
			},
			ssexp::Token::Symbol(text) if text.starts_with("Trash") => Self::Trash(text.into()),
			ssexp::Token::Symbol(text) => match &*text {
				"SaltPepperOne" => Self::SaltPepper(false),
				"SaltPepperAll" => Self::SaltPepper(true),
				"SnakeDangerAll" => Self::SnakeDangerAll,
				"Texting" => Self::Texting,
				"Glitches" => Self::Glitches,
				text => panic!("bad Rule term {text:?}"),
			},
			ssexp::Token::List(mut tokens) => {
				let ssexp::Token::Symbol(kind) = tokens.remove(0) else {
					panic!("bad rule {tokens:?}")
				};
				match &*kind {
					"and" => Self::And(tokens.into_iter().map(Self::token).collect()),
					"or" => Self::Or(tokens.into_iter().map(Self::token).collect()),
					kind => panic!("bad Rule kind {kind:?}"),
				}
			},
		}
	}

	pub fn and_trash(&mut self, term: Self) {
		if matches!(self, Self::True) {
			*self = Self::And(Vec::new());
		} else if !matches!(self, Self::And(_)) {
			*self = Self::And(vec![replace(self, Self::True)]);
		}
		let Self::And(children) = self else { unreachable!() };
		if !matches!(children.last(), Some(Self::AndTrash(_))) {
			children.push(Self::AndTrash(Vec::new()));
		}
		let Some(Self::AndTrash(trash)) = children.last_mut() else { unreachable!() };
		trash.push(term);
	}

	/// exclusively for trash regions
	pub fn or_trash(&mut self, term: Self) {
		let Self::Or(children) = self else { panic!("bad or_trash target {self:?}") };
		children.push(term);
	}
}
impl FromCell for Rule {
	fn from_cell(text: &str) -> Self {
		if text.is_empty() {
			Self::True
		} else {
			let map = ssexp::MacroMap::new().with_lists('(', ')').with_separating_whitespaces();
			let tokens =
				ssexp::parse(text.chars(), ssexp::parsers::DelimitedListParser(')'), map).unwrap();
			assert_eq!(tokens.len(), 1, "bad token count");
			Self::token(tokens.into_iter().next().unwrap())
		}
	}
}

#[derive(Clone, Copy, Debug, Serialize, PartialEq, Eq, PartialOrd, Ord, Hash)]
pub struct TrackerPosition(pub usize, pub usize);
impl FromCell for TrackerPosition {
	fn from_cell(text: &str) -> Self {
		let (left, right) = text.split_once(',').expect("TrackerPosition");
		Self(left.parse().unwrap(), right.parse().unwrap())
	}
}

#[derive(Clone, Copy, Debug, Serialize, PartialEq, Eq, PartialOrd, Ord, Hash)]
#[serde(transparent)]
pub struct RegionIndex(pub i32);
impl From<RegionIndex> for i32 {
	fn from(value: RegionIndex) -> Self { value.0 }
}
impl From<i32> for RegionIndex {
	fn from(value: i32) -> Self { Self(value) }
}

#[derive(Clone, Copy, Debug, Serialize, PartialEq, Eq, PartialOrd, Ord, Hash)]
#[serde(transparent)]
pub struct LocationIndex(pub i32);
impl From<LocationIndex> for i32 {
	fn from(value: LocationIndex) -> Self { value.0 }
}
impl From<i32> for LocationIndex {
	fn from(value: i32) -> Self { Self(value) }
}

#[derive(Clone, Copy, Debug, Serialize, PartialEq, Eq, PartialOrd, Ord, Hash)]
#[serde(transparent)]
pub struct ItemIndex(pub i32);
impl From<ItemIndex> for i32 {
	fn from(value: ItemIndex) -> Self { value.0 }
}
impl From<i32> for ItemIndex {
	fn from(value: i32) -> Self { Self(value) }
}

#[derive(Debug)]
pub enum LocationData {
	None,
	Event(String),
	TrashType(String),
}

#[derive(Clone, Copy, Debug, Serialize, PartialEq, Eq, PartialOrd, Ord, Hash)]
pub enum LocationType {
	// order is used for tracker position
	TrashType,
	Trash,
	Achievement,
	Catapult,
	SnakeDanger,
	SaltAndPepper,
	Victory,
	Segment,
	Delivery,
}
impl FromCell for LocationType {
	fn from_cell(text: &str) -> Self {
		match text {
			"Victory" => Self::Victory,
			"Segment" => Self::Segment,
			"Delivery" => Self::Delivery,
			"Achievement" => Self::Achievement,
			"Catapult" => Self::Catapult,
			"SnakeDanger" => Self::SnakeDanger,
			"SaltAndPepper" => Self::SaltAndPepper,
			"Trash" => Self::Trash,
			"TrashType" => Self::TrashType,
			text => panic!("bad LocationType {text:?}"),
		}
	}
}

#[derive(Clone, Copy, Debug, Serialize_repr)]
#[repr(i8)]
pub enum ItemClass {
	Filler = 0,
	Progression = 1,
	Useful = 2,
	ProgressionUseful = 3,
	Trap = 4,
	UsefulTrap = 6,
	BossfightProgression = -1,
}
impl FromCell for ItemClass {
	fn from_cell(text: &str) -> Self {
		match text {
			"Filler" => Self::Filler,
			"Progression" => Self::Progression,
			"Useful" => Self::Useful,
			"ProgressionUseful" => Self::ProgressionUseful,
			"Trap" => Self::Trap,
			"UsefulTrap" => Self::UsefulTrap,
			"BossfightProgression" => Self::BossfightProgression,
			text => panic!("bad ItemClass {text:?}"),
		}
	}
}

#[derive(Clone, Debug, PartialEq, Eq, PartialOrd, Ord)]
pub enum ItemType {
	Filler(String),
	Level(usize),
	TrashType,
	HoleGlobal,
	HoleSplit,
	CatapultGlobal,
	CatapultSplit,
	Texting,
	Piece,
	SnakeDanger,
	SaltAndPepper,
	Debug,
}
impl FromCell for ItemType {
	fn from_cell(text: &str) -> Self {
		if let Some(kind) = text.strip_prefix("Filler;") {
			Self::Filler(kind.into())
		} else if let Some(kind) = text.strip_prefix("Level;") {
			Self::Level(usize::from_cell(kind))
		} else {
			match text {
				"HoleGlobal" => Self::HoleGlobal,
				"HoleSplit" => Self::HoleSplit,
				"CatapultGlobal" => Self::CatapultGlobal,
				"CatapultSplit" => Self::CatapultSplit,
				"Texting" => Self::Texting,
				"Piece" => Self::Piece,
				"SnakeDanger" => Self::SnakeDanger,
				"SaltAndPepper" => Self::SaltAndPepper,
				"Debug" => Self::Debug,
				text => panic!("bad ItemType {text:?}"),
			}
		}
	}
}

pub trait IdMapKey: Copy + From<i32> + Into<i32> + fmt::Debug {}
impl<T: Copy + From<i32> + Into<i32> + fmt::Debug> IdMapKey for T {}

pub struct IdMap<K, V> {
	index: Vec<Option<V>>,
	name: HashMap<String, K>,
}

impl<K: IdMapKey, V> Default for IdMap<K, V> {
	fn default() -> Self { Self::new() }
}

impl<K: IdMapKey, V: fmt::Debug> fmt::Debug for IdMap<K, V> {
	fn fmt(&self, f: &mut fmt::Formatter) -> fmt::Result {
		struct InnerValue<'data, V>(usize, Option<&'data str>, Option<&'data V>);
		impl<V: fmt::Debug> fmt::Debug for InnerValue<'_, V> {
			fn fmt(&self, f: &mut fmt::Formatter) -> fmt::Result {
				write!(f, "{}", self.0)?;
				if let Some(name) = self.1 {
					write!(f, " {name:?}")?;
				}
				if let Some(value) = self.2 {
					f.write_str(": ")?;
					fmt::Debug::fmt(value, f)?;
				}
				Ok(())
			}
		}
		let mut names = vec![None::<&str>; self.index.len()];
		for (name, &k) in &self.name {
			names[k.into() as usize] = Some(name);
		}
		f.debug_set()
			.entries(
				self.index
					.iter()
					.zip(names)
					.enumerate()
					.map(|(i, (value, name))| InnerValue(i, name, value.as_ref())),
			)
			.finish()
	}
}

impl<K: IdMapKey, V> IdMap<K, V> {
	pub fn new() -> Self { Self { index: Vec::new(), name: HashMap::new() } }

	pub fn iter(&self) -> impl Iterator<Item = (K, &V)> {
		self.index
			.iter()
			.enumerate()
			.filter_map(|(i, value)| value.as_ref().map(|value| (K::from(i as i32), value)))
	}

	pub fn len(&self) -> usize { self.index.len() }

	pub fn get(&self, index: K) -> Option<&V> {
		self.index.get(index.into() as usize).map(Option::as_ref).flatten()
	}

	pub fn name(&self, name: &str) -> Option<K> { self.name.get(name).copied() }

	pub fn get_mut(&mut self, index: K) -> Option<&mut V> {
		self.index.get_mut(index.into() as usize).map(Option::as_mut).flatten()
	}

	pub fn set(&mut self, name: Option<String>, index: K, value: V) {
		if let Some(name) = name {
			self.name.insert(name, index);
		}
		let index_into = index.into() as usize;
		self.index.resize_with(self.index.len().max(index_into + 1), || None);
		if self.index[index_into].replace(value).is_some() {
			panic!("duplicate entry at {index:?}");
		}
	}

	pub fn get_named(&self, name: &str) -> &V {
		&self[self.name(name).unwrap_or_else(|| panic!("no name {name:?} in map"))]
	}

	pub fn add_sequential(&mut self, name: Option<String>, value: V) -> K {
		let index = K::from(self.index.len() as i32);
		self.set(name, index, value);
		index
	}

	pub fn find_gaps(&self) -> impl Iterator<Item = K> {
		self.index
			.iter()
			.enumerate()
			.filter_map(|(i, value)| value.is_none().then_some(i))
			.chain([self.index.len()])
			.map(|index| K::from(index as i32))
	}
}
impl<K: IdMapKey, V> ops::Index<K> for IdMap<K, V> {
	type Output = V;

	fn index(&self, index: K) -> &Self::Output {
		self.get(index).unwrap_or_else(|| panic!("no id {index:?} in map"))
	}
}
impl<K: IdMapKey, V> ops::IndexMut<K> for IdMap<K, V> {
	fn index_mut(&mut self, index: K) -> &mut Self::Output {
		self.get_mut(index).unwrap_or_else(|| panic!("no id {index:?} in map"))
	}
}

#[derive(Clone, Debug)]
pub struct RegionConnection {
	pub to: RegionIndex,
	pub name: String,
	pub rules: Rule,
}

#[derive(Debug)]
pub struct Region {
	pub name: String,
	pub next: Option<RegionConnection>,
	pub connections: BTreeMap<RegionIndex, RegionConnection>,
	pub position: BTreeSet<TrackerPosition>,
}

#[derive(Debug)]
pub struct Location {
	pub name: String,
	pub data: LocationData,
	pub r#type: LocationType,
	pub region: RegionIndex,
	pub rules: Rule,
}

#[derive(Debug)]
pub struct Item {
	pub id: String,
	pub name: String,
	pub count: usize,
	pub class: ItemClass,
	pub r#type: ItemType,
	pub groups: Vec<String>,
}

#[derive(Debug)]
pub struct Level {
	pub name: String,
	pub entrance: RegionIndex,
	pub unlock: ItemIndex,
}

#[derive(Clone, Debug)]
pub struct Trash {
	pub name: String,
	pub r#type: Option<String>,
	pub location: Option<LocationIndex>,
}

#[derive(Debug)]
pub struct Scene {
	pub name: String,
	pub trash: Vec<Trash>,
}

#[derive(Debug, Default, PartialEq, Eq)]
pub struct Version(pub i32, pub i32, pub i32, pub i32);
impl Version {
	pub fn ap_string(&self) -> String { format!("{}.{}.{}", self.0, self.1, self.2) }

	pub fn string(&self) -> String {
		assert_ne!(self.3, 0, "invalid preview 0 {self:?}");
		if self.3 > 0 {
			format!("{}.{}.{}.{}", self.0, self.1, self.2, self.3)
		} else {
			self.ap_string()
		}
	}

	pub fn user_string(&self) -> String {
		assert_ne!(self.3, 0, "invalid preview 0 {self:?}");
		if self.3 > 0 {
			format!("v{}.{}.{}-preview.{}", self.0, self.1, self.2, self.3)
		} else {
			format!("v{}.{}.{}", self.0, self.1, self.2)
		}
	}
}
impl FromCell for Version {
	fn from_cell(text: &str) -> Self {
		let mut iter = text.split('.');
		let major = i32::from_cell(iter.next().unwrap());
		let minor = i32::from_cell(iter.next().unwrap());
		let patch = i32::from_cell(iter.next().unwrap());
		let prerelease = iter.next().map(i32::from_cell).unwrap_or(-1);
		assert_eq!(iter.next(), None, "overlong version");
		Self(major, minor, patch, prerelease)
	}
}

#[derive(Debug, Default)]
pub struct Data {
	// i've got nowhere better to put it so here's my unified version algorithm:
	// - let client_version, compat_version, server_version (version reported by slotdata)
	// - versions are either stable (major.minor.patch) or unstable (major.minor.patch.preview)
	// - if server_version is unstable && client_version != server_version, warn "Server {server_version} is unstable and different from client {client_version}"
	// - if server_version < compat_version, warn "Server {server_version} is older than minimum supported {compat_version} of client {client_version}"
	// - if server_version is stable && client_version is unstable && server_version >= stable client_version (so 0.2.0.1 < 0.2.0), error "Server {server_version} is newer than client {client_version}, please update your client"
	// - if server_version > client_version, error "Server {server_version} is newer than client {client_version}, please update your client"
	// - when displaying version to user, v#.#.# or v#.#.#-preview.#
	pub version: Version,
	pub compat_version: Version,
	pub ap_version: String,
	pub authors: Vec<String>,
	// TODO: these are basically three completely different structures in usage
	pub regions: IdMap<RegionIndex, Region>,
	pub locations: IdMap<LocationIndex, Location>,
	pub items: IdMap<ItemIndex, Item>,
	pub sorted_items: Vec<ItemIndex>,
	pub sorted_locations: Vec<LocationIndex>,
	pub levels: Vec<Level>,
	pub scenes: BTreeMap<String, Scene>,
}

impl Data {
	pub fn fill_sorted_locations(&mut self) {
		self.sorted_locations = self.locations.iter().map(|(index, _)| index).collect();
		self.sorted_locations.sort_by_cached_key(|&index| {
			let location = self.locations.get(index).unwrap();
			let trash_name =
				if matches!(location.r#type, LocationType::Trash) { &location.name } else { "" };
			(
				self.regions.get(location.region).unwrap().position.first().unwrap(),
				location.r#type,
				trash_name,
			)
		});
	}
}
