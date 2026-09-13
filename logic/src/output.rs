use std::collections::{BTreeMap, BTreeSet};
use std::fmt;

use hashbrown::{HashMap, HashSet};
use serde::Serialize;
use serde_tuple::Serialize_tuple;

use crate::data::{
	Data, ItemClass, ItemIndex, ItemType, LocationData, LocationIndex, LocationType, RegionIndex,
	Rule, TrackerPosition,
};

#[derive(Clone, Debug, Serialize, PartialEq, Eq, Hash)]
pub struct PyOptionFilter {
	option: &'static str,
	value: usize,
	operator: &'static str,
}
impl PyOptionFilter {
	fn hole(value: usize) -> Self {
		PyOptionFilter { option: "worlds.donutcounty.options.Hole", value, operator: "eq" }
	}

	fn catapult(value: usize, operator: &'static str) -> Self {
		PyOptionFilter { option: "worlds.donutcounty.options.Catapult", value, operator }
	}

	fn trash_souls() -> Self {
		PyOptionFilter { option: "worlds.donutcounty.options.TrashSouls", value: 1, operator: "eq" }
	}

	fn trashsanity(value: usize, operator: &'static str) -> Self {
		PyOptionFilter { option: "worlds.donutcounty.options.Trashsanity", value, operator }
	}

	fn achievements() -> Self {
		PyOptionFilter {
			option: "worlds.donutcounty.options.Achievements",
			value: 1,
			operator: "eq",
		}
	}

	fn salt_and_pepper() -> Self {
		PyOptionFilter {
			option: "worlds.donutcounty.options.SaltAndPepper",
			value: 1,
			operator: "eq",
		}
	}

	fn snake_danger() -> Self {
		PyOptionFilter {
			option: "worlds.donutcounty.options.SnakeDanger",
			value: 1,
			operator: "eq",
		}
	}

	fn texting() -> Self {
		PyOptionFilter { option: "worlds.donutcounty.options.Texting", value: 1, operator: "eq" }
	}
}

#[derive(Clone, Debug, Serialize, PartialEq, Eq, Hash)]
pub struct PyRuleHas<'data> {
	item_name: &'data str,
	count: usize,
}

#[derive(Clone, Debug, Serialize, PartialEq, Eq, Hash)]
pub struct PyLevelPieces {
	index: usize,
}

#[derive(Clone, Debug, Serialize, PartialEq, Eq, Hash)]
#[serde(tag = "rule")]
pub enum PyRule<'data> {
	Has {
		#[serde(skip_serializing_if = "Vec::is_empty")]
		options: Vec<PyOptionFilter>,
		//#[serde(skip_serializing_if = "std::ops::Not::not")]
		filtered_resolution: bool,
		args: PyRuleHas<'data>,
	},
	And {
		#[serde(skip_serializing_if = "Vec::is_empty")]
		options: Vec<PyOptionFilter>,
		//#[serde(skip_serializing_if = "std::ops::Not::not")]
		filtered_resolution: bool,
		children: Vec<PyRule<'data>>,
	},
	Or {
		children: Vec<PyRule<'data>>,
	},
	#[serde(rename = "True_")]
	True,
	/// custom rule
	LevelPieces {
		args: PyLevelPieces,
	},
}

impl<'data> PyRule<'data> {
	fn has(name: &'data str, count: usize, options: Vec<PyOptionFilter>) -> Self {
		Self::Has { options, args: PyRuleHas { item_name: name, count }, filtered_resolution: true }
	}

	fn new(data: &'data Data, rule: &'data Rule) -> Self {
		match rule {
			Rule::Hole(kind) => Self::And {
				children: vec![
					Self::has(&data.items.get_named("Hole").name, 1, vec![PyOptionFilter::hole(1)]),
					Self::has(&data.items.get_named(kind).name, 1, vec![PyOptionFilter::hole(2)]),
				],
				options: Vec::new(),
				filtered_resolution: true,
			},
			Rule::Catapult(kind) => Self::And {
				children: vec![
					Self::has(&data.items.get_named("Catapult").name, 1, vec![
						PyOptionFilter::catapult(1, "eq"),
					]),
					Self::has(&data.items.get_named(kind).name, 1, vec![PyOptionFilter::catapult(
						2, "eq",
					)]),
				],
				options: Vec::new(),
				filtered_resolution: true,
			},
			Rule::Trash(kind) => {
				Self::has(&data.items.get_named(kind).name, 1, vec![PyOptionFilter::trash_souls()])
			},
			&Rule::Level(index) => Self::LevelPieces { args: PyLevelPieces { index } },
			&Rule::SaltPepper(all) => Self::And {
				children: vec![
					Self::has(&data.items.get_named("Salt").name, if all { 2 } else { 1 }, vec![
						PyOptionFilter::salt_and_pepper(),
					]),
					Self::has(&data.items.get_named("Pepper").name, if all { 3 } else { 1 }, vec![
						PyOptionFilter::salt_and_pepper(),
					]),
				],
				options: Vec::new(),
				filtered_resolution: true,
			},
			Rule::SnakeDangerAll => Self::has(&data.items.get_named("SnakeDanger").name, 4, vec![
				PyOptionFilter::snake_danger(),
			]),
			Rule::Texting => {
				Self::has(&data.items.get_named("Texting").name, 1, vec![PyOptionFilter::texting()])
			},
			Rule::Glitches => Self::has("Glitches", 1, Vec::new()),
			// since the child term is created by Self::new, it'll never have a double-nest
			Rule::And(terms) => {
				let mut children = Vec::new();
				let mut seen = HashSet::new();
				for term in terms.iter().map(|term| Self::new(data, term)).flat_map(|term| {
					if let Self::And { children, options, .. } = &term
						&& options.is_empty()
					{
						children.clone()
					} else {
						vec![term]
					}
				}) {
					if !matches!(term, Self::True) && seen.insert(term.clone()) {
						children.push(term);
					}
				}
				if children.is_empty() {
					Self::True
				} else {
					Self::And { children, options: Vec::new(), filtered_resolution: true }
				}
			},
			Rule::AndTrash(terms) => {
				let mut children = Vec::new();
				let mut seen = HashSet::new();
				for term in terms.iter().map(|term| Self::new(data, term)).flat_map(|term| {
					if let Self::And { children, .. } = term { children } else { vec![term] }
				}) {
					if !matches!(term, Self::True) && seen.insert(term.clone()) {
						children.push(term);
					}
				}
				if children.is_empty() {
					Self::True
				} else {
					// TODO: instead cascade filter to children?
					Self::And {
						children,
						options: vec![PyOptionFilter::trash_souls()],
						filtered_resolution: true,
					}
				}
			},
			Rule::Or(terms) => {
				let mut children = Vec::new();
				let mut seen = HashSet::new();
				for term in terms.iter().map(|term| Self::new(data, term)).flat_map(|term| {
					if let Self::Or { children } = term { children } else { vec![term] }
				}) {
					if matches!(term, Self::True) {
						return Self::True;
					}
					if seen.insert(term.clone()) {
						children.push(term);
					}
				}
				Self::Or { children }
			},
			Rule::True => Self::True,
		}
	}
}

#[derive(Debug, Serialize_tuple)]
pub struct PyEntrance<'data> {
	from: RegionIndex,
	to: RegionIndex,
	name: &'data str,
	rules: PyRule<'data>,
}

#[derive(Debug, Serialize_tuple)]
pub struct PyLocation<'data> {
	id: LocationIndex,
	name: &'data str,
	region: RegionIndex,
	rules: PyRule<'data>,
}

#[derive(Debug, Serialize_tuple)]
pub struct PyLocationGroup<'data> {
	filter: Option<PyOptionFilter>,
	locations: Vec<PyLocation<'data>>,
}

#[derive(Debug, Serialize_tuple)]
pub struct PyItem<'data> {
	name: &'data str,
	count: usize,
}

#[derive(Debug, Serialize_tuple)]
pub struct PyItemGroup<'data> {
	filter: Option<PyOptionFilter>,
	items: Vec<PyItem<'data>>,
}

#[derive(Debug, Serialize)]
pub struct PyArchipelago<'data> {
	game: &'static str,
	minimum_ap_version: &'data str,
	world_version: String,
	authors: &'data [String],
}

impl<'data> PyArchipelago<'data> {
	pub fn new(data: &'data Data) -> Self {
		Self {
			game: "Donut County",
			minimum_ap_version: &data.ap_version,
			world_version: data.version.ap_string(),
			authors: &data.authors,
		}
	}
}

#[derive(Debug, Serialize)]
#[expect(non_snake_case, reason = "match with region ids")]
pub struct PyData<'data> {
	version: String,
	compat_version: String,
	regions: Vec<&'data str>,
	entrances: Vec<PyEntrance<'data>>,
	locations: Vec<PyLocationGroup<'data>>,
	location_to_id: BTreeMap<&'data str, LocationIndex>,
	location_to_sort: BTreeMap<&'data str, usize>,
	location_groups: BTreeMap<&'data str, Vec<&'data str>>,
	items: Vec<PyItemGroup<'data>>,
	item_to_id: BTreeMap<&'data str, ItemIndex>,
	item_to_class: BTreeMap<&'data str, ItemClass>,
	item_groups: BTreeMap<&'data str, Vec<&'data str>>,
	fillers: BTreeMap<&'data str, Vec<&'data str>>,
	// script-referenced ids
	level_items: Vec<Option<&'data str>>,
	piece: &'data str,
	aCatapult: RegionIndex,
	aWin: RegionIndex,
}

impl<'data> PyData<'data> {
	pub fn new(data: &'data Data) -> Self {
		let regions = data.regions.iter().map(|(_, region)| &*region.name).collect();
		let entrances = data
			.regions
			.iter()
			.flat_map(|(index, region)| {
				region.next.iter().chain(region.connections.values()).map(move |connection| {
					PyEntrance {
						from: index,
						to: connection.to,
						name: &connection.name,
						// TODO: this is janky
						rules: PyRule::new(data, &connection.rules),
					}
				})
			})
			.collect();
		let mut locations = BTreeMap::<LocationType, Vec<PyLocation>>::new();
		let mut location_to_id = BTreeMap::new();
		let mut location_to_sort = BTreeMap::new();
		let mut location_groups = BTreeMap::<&'data str, Vec<&'data str>>::new();
		for (index, location) in data.locations.iter() {
			let group = match location.r#type {
				LocationType::Victory => continue,
				LocationType::Segment => "Segment",
				LocationType::Delivery => "Level",
				LocationType::Achievement => "Achievement",
				LocationType::Catapult => "Element",
				LocationType::SnakeDanger => "Element",
				LocationType::SaltAndPepper => "Element",
				LocationType::TrashType => "Trash",
				LocationType::Trash => "Trash",
			};
			location_groups.entry(group).or_default().push(&location.name);
			locations.entry(location.r#type).or_default().push(PyLocation {
				id: index,
				name: &location.name,
				region: location.region,
				rules: PyRule::new(data, &location.rules),
			});
			assert!(!location.name.is_empty(), "empty location name for {index:?}");
			if location_to_id.insert(&*location.name, index).is_some() {
				panic!("duplicate location {:?}", location.name);
			}
		}
		let locations = locations
			.into_iter()
			.map(|(r#type, locations)| PyLocationGroup {
				filter: match r#type {
					LocationType::TrashType => Some(PyOptionFilter::trashsanity(1, "eq")),
					LocationType::Trash => Some(PyOptionFilter::trashsanity(2, "eq")),
					LocationType::Achievement => Some(PyOptionFilter::achievements()),
					LocationType::Catapult => Some(PyOptionFilter::catapult(0, "ne")),
					LocationType::SnakeDanger => Some(PyOptionFilter::snake_danger()),
					LocationType::SaltAndPepper => Some(PyOptionFilter::salt_and_pepper()),
					LocationType::Victory => unreachable!(),
					LocationType::Segment => None,
					LocationType::Delivery => None,
				},
				locations,
			})
			.collect();
		for (i, &location) in data.sorted_locations.iter().enumerate() {
			location_to_sort.insert(&*data.locations.get(location).unwrap().name, i);
		}
		let mut items = BTreeMap::<ItemType, Vec<PyItem>>::new();
		let mut item_to_id = BTreeMap::new();
		let mut item_to_class = BTreeMap::new();
		let mut item_groups = BTreeMap::<&'data str, Vec<&'data str>>::new();
		let mut fillers = BTreeMap::<&'data str, Vec<&'data str>>::new();
		for (index, item) in data.items.iter() {
			assert!(!item.name.is_empty(), "empty item name for {index:?}");
			if item_to_id.insert(&*item.name, index).is_some() {
				panic!("duplicate item {:?}", item.name);
			}
			item_to_class.insert(&*item.name, item.class);
			if let ItemType::Filler(id) = &item.r#type {
				fillers.entry(id).or_default().push(&item.name);
			}
			for group in &item.groups {
				item_groups.entry(group).or_default().push(&item.name);
			}
			let fixed_type = match &item.r#type {
				ItemType::Filler(_) => ItemType::Filler(String::new()),
				ItemType::Level(_) => ItemType::Level(0),
				other => other.clone(),
			};
			items
				.entry(fixed_type)
				.or_default()
				.push(PyItem { name: &item.name, count: item.count });
		}
		let items = items
			.into_iter()
			.map(|(r#type, items)| PyItemGroup {
				filter: match r#type {
					ItemType::Filler(_) | ItemType::Level(_) | ItemType::Piece => None,
					ItemType::TrashType => Some(PyOptionFilter::trash_souls()),
					ItemType::HoleGlobal => Some(PyOptionFilter::hole(1)),
					ItemType::HoleSplit => Some(PyOptionFilter::hole(2)),
					ItemType::CatapultGlobal => Some(PyOptionFilter::catapult(1, "eq")),
					ItemType::CatapultSplit => Some(PyOptionFilter::catapult(2, "eq")),
					ItemType::Texting => Some(PyOptionFilter::texting()),
					ItemType::SnakeDanger => Some(PyOptionFilter::snake_danger()),
					ItemType::SaltAndPepper => Some(PyOptionFilter::salt_and_pepper()),
					ItemType::Debug => None,
				},
				items,
			})
			.collect();
		let level_items = data
			.levels
			.iter()
			.map(|level| data.items.get(level.unlock).map(|item| &*item.name))
			.collect();
		Self {
			version: data.version.string(),
			compat_version: data.compat_version.string(),
			regions,
			entrances,
			locations,
			location_to_id,
			location_to_sort,
			location_groups,
			items,
			item_to_id,
			item_to_class,
			item_groups,
			fillers,
			level_items,
			piece: &data.items.get_named("QuadcopterPiece").name,
			aCatapult: data.regions.name("aCatapult").unwrap(),
			aWin: data.regions.name("aWin").unwrap(),
		}
	}
}

#[derive(Clone, Debug, Serialize_tuple)]
pub struct GameLocation {
	id: LocationIndex,
	r#type: LocationType,
}

#[derive(Clone, Debug, Serialize_tuple)]
pub struct GameDebugLocation<'data> {
	id: LocationIndex,
	r#type: LocationType,
	name: &'data str,
}

#[derive(Debug, Serialize_tuple)]
pub struct GameTrash<'data> {
	name: &'data str,
	#[serde(skip_serializing_if = "Option::is_none")]
	unlock: Option<ItemIndex>,
	#[serde(skip_serializing_if = "Option::is_none")]
	type_id: Option<LocationIndex>,
	#[serde(skip_serializing_if = "Option::is_none")]
	id: Option<LocationIndex>,
}

#[derive(Clone, Debug, Default, Serialize)]
#[serde(rename_all = "PascalCase")]
pub struct GameSegment {
	locations: Vec<GameLocation>,
	trash: usize,
	types: usize,
}

#[derive(Debug, Serialize)]
#[serde(rename_all = "PascalCase")]
pub struct GameLevel {
	unlock: ItemIndex,
	segments: Vec<GameSegment>,
}

#[derive(Debug, Serialize)]
#[serde(rename_all = "PascalCase")]
pub struct GameData<'data> {
	events: BTreeMap<&'data str, GameLocation>,
	locations_size: usize,
	trash: BTreeMap<&'data str, Vec<GameTrash<'data>>>,
	levels: Vec<GameLevel>,
	trash_events: BTreeMap<LocationIndex, BTreeSet<usize>>,
	counters: Vec<usize>,
	debug_sorted_items: Vec<ItemIndex>,
	debug_sorted_locations: Vec<GameDebugLocation<'data>>,
}

impl<'data> GameData<'data> {
	pub fn new(data: &'data Data) -> Self {
		let events = data
			.locations
			.iter()
			.filter_map(|(index, location)| {
				if let LocationData::Event(event) = &location.data {
					Some((&**event, GameLocation { id: index, r#type: location.r#type }))
				} else {
					None
				}
			})
			.collect();
		let mut levels = data
			.levels
			.iter()
			.map(|level| GameLevel { unlock: level.unlock, segments: Vec::new() })
			.collect::<Vec<_>>();
		let mut counters = Vec::new();
		let mut counters_map = HashMap::new();
		let mut trash_events = BTreeMap::new();
		for &index in &data.sorted_locations {
			let location = data.locations.get(index).unwrap();
			let region = data.regions.get(location.region).unwrap();
			match location.r#type {
				LocationType::Trash => {
					assert_eq!(region.position.len(), 1, "multiregion trash!");
					let position = region.position.first().unwrap();
					let segments = &mut levels[position.0].segments;
					segments.resize(segments.len().max(position.1 + 1), GameSegment::default());
					let tracker = *counters_map.entry((position, true)).or_insert_with(|| {
						let len = counters.len();
						counters.push(0);
						len
					});
					counters[tracker] += 1;
					segments[position.1].trash = tracker;
					trash_events.insert(index, [tracker].into_iter().collect());
				},
				LocationType::TrashType => {
					trash_events.insert(
						index,
						region
							.position
							.iter()
							.map(|position| {
								let segments = &mut levels[position.0].segments;
								segments.resize(
									segments.len().max(position.1 + 1),
									GameSegment::default(),
								);
								let tracker =
									*counters_map.entry((position, false)).or_insert_with(|| {
										let len = counters.len();
										counters.push(0);
										len
									});
								counters[tracker] += 1;
								segments[position.1].types = tracker;
								tracker
							})
							.collect(),
					);
				},
				_ => {
					for position in &region.position {
						let segments = &mut levels[position.0].segments;
						segments.resize(segments.len().max(position.1 + 1), GameSegment::default());
						segments[position.1]
							.locations
							.push(GameLocation { id: index, r#type: location.r#type });
					}
				},
			};
		}
		Self {
			events,
			locations_size: data.locations.len(),
			trash: data
				.scenes
				.iter()
				.map(|(name, scene)| {
					(
						&**name,
						scene
							.trash
							.iter()
							.map(|trash| GameTrash {
								name: &trash.name,
								unlock: trash.r#type.as_ref().map(|r#type| {
									data.items.name(&format!("Trash{type}")).unwrap()
								}),
								type_id: trash.r#type.as_ref().map(|r#type| {
									data.locations.name(&format!("Trash{type}")).unwrap()
								}),
								id: trash.location,
							})
							.collect(),
					)
				})
				.collect(),
			levels,
			trash_events,
			counters,
			debug_sorted_items: data.sorted_items.clone(),
			debug_sorted_locations: data
				.sorted_locations
				.iter()
				.map(|&index| {
					let location = data.locations.get(index).unwrap();
					GameDebugLocation { id: index, r#type: location.r#type, name: &location.name }
				})
				.collect(),
		}
	}
}

#[derive(Debug)]
pub struct GameDataItemIds<'data> {
	entries: Vec<(&'data str, ItemIndex)>,
}

impl<'data> GameDataItemIds<'data> {
	pub fn new(data: &'data Data) -> Self {
		Self {
			entries: [("None", ItemIndex(0))]
				.into_iter()
				.chain(data.items.iter().map(|(index, item)| (&*item.id, index)))
				.collect(),
		}
	}
}

impl fmt::Display for GameDataItemIds<'_> {
	fn fmt(&self, f: &mut fmt::Formatter) -> fmt::Result {
		writeln!(f, "namespace DonutCountyAP.Generated;")?;
		writeln!(f, "public enum ItemId {{")?;
		let mut expected = 0;
		for &(name, ItemIndex(index)) in &self.entries {
			write!(f, "    {name}")?;
			if index != expected {
				expected = index;
				write!(f, " = {index}");
			}
			writeln!(f, ",")?;
			expected += 1;
		}
		writeln!(f, "    Length")?;
		writeln!(f, "}}")
	}
}

#[derive(Debug)]
pub struct GameDataVersionInfo<'data> {
	version: String,
	compat_version: String,
	text_version: String,
	ap_version: &'data str,
}

impl<'data> GameDataVersionInfo<'data> {
	pub fn new(data: &'data Data) -> Self {
		Self {
			version: data.version.string(),
			compat_version: data.compat_version.string(),
			text_version: data.version.user_string(),
			ap_version: &data.ap_version,
		}
	}
}

impl fmt::Display for GameDataVersionInfo<'_> {
	fn fmt(&self, f: &mut fmt::Formatter) -> fmt::Result {
		writeln!(f, "namespace DonutCountyAP.Generated;")?;
		writeln!(f, "public class VersionInfo {{")?;
		writeln!(f, "    public const string VERSION = {:?};", self.version)?;
		writeln!(f, "    public const string COMPAT_VERSION = {:?};", self.compat_version)?;
		writeln!(f, "    public const string TEXT_VERSION = {:?};", self.text_version)?;
		writeln!(f, "    public const string AP_VERSION = {:?};", self.ap_version)?;
		writeln!(f, "}}")
	}
}
