#![expect(unused, reason = "not done yet")]
use std::collections::{BTreeMap, BTreeSet};
use std::fs::File;
use std::io::Write;
use std::mem::take;

use iter_debug::DebugIterator;

use crate::data::{
	Data, Item, ItemClass, ItemIndex, ItemType, Level, Location, LocationIndex, LocationType,
	Region, RegionConnection, RegionIndex, Rule, Scene, TrackerPosition, Trash, Version,
};
use crate::output::{GameData, GameDataItemIds, GameDataVersionInfo, PyArchipelago, PyData};
use crate::sheet::{FromCell, SheetReader};

mod data;
mod output;
mod sheet;

enum PlaceholderRegionConnection {
	Texting(String),
	Next(String, Rule),
	Levels,
}
impl FromCell for PlaceholderRegionConnection {
	fn from_cell(text: &str) -> Self {
		match text {
			"Texting" => Self::Texting(String::new()),
			"Next" => Self::Next(String::new(), Rule::True),
			"Levels" => Self::Levels,
			text => panic!("bad PlaceholderRegionConnection {text:?}"),
		}
	}
}

pub enum TrashTypeExtra {
	Event,
	Start
}
impl FromCell for TrashTypeExtra {
	fn from_cell(text: &str) -> Self {
		match text {
			"Event" => Self::Event,
			"Start" => Self::Start,
			text => panic!("bad TrashTypeExtra {text:?}"),
		}
	}
}

fn main() {
	let mut data = Data::default();
	// TODO: replace with a pull-based system since sheets need to be ordered anyways
	SheetReader::read_book("../logic.xml", |reader, name| match name {
		"metadata" => reader.sheet(worksheet!(|field: String,
								 version: Option<Version>,
								 (mut) ap_meta: Vec<String>| {
			match &*field {
				"version" => {
					data.version = version.unwrap()
				},
				"compat" => {
					data.compat_version = version.unwrap()
				},
				"archipelago" => {
					data.ap_version = ap_meta.remove(0);
					data.authors = ap_meta;
				},
				name => panic!("bad metadata {name:?}"),
			}
		})),
		"regions" => {
			let mut prev_level_name = String::new();
			let mut connections = Vec::new();
			reader.sheet(worksheet!(|location: Option<i32>,
			                         event: Option<String>,
			                         position: Vec<TrackerPosition>,
			                         // defer typecheck until i know it's a location
			                         r#type: String,
			                         id: String,
			                         connection: Vec<PlaceholderRegionConnection>,
			                         level_name: Option<String>,
			                         name: String,
			                         rules: Rule| {
				let is_new_level = level_name.is_some();
				let level_name = level_name.unwrap_or_else(|| take(&mut prev_level_name));
				prev_level_name = level_name.clone();
				let region_name = if matches!(&*r#type, "Global" | "Victory") {
					name
				} else {
					format!("{level_name}: {name}")
				};
				let region = Region {
					name: region_name.clone(),
					next: None,
					connections: BTreeMap::new(),
					position: position.into_iter().collect(),
				};
				let region_index = data.regions.add_sequential(Some(id.clone()), region);
				if is_new_level {
					data.levels.push(Level {
						name: level_name.clone(),
						entrance: region_index,
						unlock: ItemIndex(-1),
					});
				} else if r#type == "Victory" {
					data.levels.push(Level {
						name: region_name.clone(),
						entrance: region_index,
						unlock: ItemIndex(0),
					});
				}
				if matches!(&*r#type, "Segment") {
					connections.push((
						region_index,
						PlaceholderRegionConnection::Next(region_name.clone(), rules.clone()),
					));
				}
				for mut connection in connection {
					match &mut connection {
						PlaceholderRegionConnection::Texting(name) => *name = level_name.clone(),
						PlaceholderRegionConnection::Next(name, rules) => {
							*name = region_name.clone();
							*rules = rules.clone();
						},
						PlaceholderRegionConnection::Levels => {},
					}
					connections.push((region_index, connection));
				}
				if let Some(location) = location {
					let location_type = LocationType::from_cell(&r#type);
					let location_name = if matches!(location_type, LocationType::Delivery) {
						level_name
					} else {
						region_name
					};
					data.locations.set(Some(id), LocationIndex(location), Location {
						name: location_name,
						event,
						r#type: location_type,
						region: region_index,
						rules,
					});
				}
			}));
			for (index, connection) in connections {
				match connection {
					PlaceholderRegionConnection::Texting(level) => {
						let to = data.regions.name("texting").unwrap();
						data.regions.get_mut(index).unwrap().connections.insert(
							to,
							RegionConnection {
								to,
								name: format!("Texting in {level}"),
								rules: Rule::True,
							},
						);
					},
					PlaceholderRegionConnection::Next(segment, rules) => {
						data.regions.get_mut(index).unwrap().next = Some(RegionConnection {
							to: RegionIndex(index.0 + 1),
							name: format!("Complete {segment}"),
							rules,
						});
					},
					PlaceholderRegionConnection::Levels => {
						let connections = data.levels.iter().enumerate().map(|(i, level)| {
							(level.entrance, RegionConnection {
								to: level.entrance,
								name: format!("Enter {}", level.name),
								rules: Rule::Level(i),
							})
						});
						data.regions.get_mut(index).unwrap().connections.extend(connections);
					},
				}
			}
		},
		"misc_locations" => {
			reader.sheet(worksheet!(|location: i32,
			                         event: Option<String>,
			                         r#type: LocationType,
			                         name: String,
			                         region: String,
			                         rules: Rule| {
				data.locations.set(None, LocationIndex(location), Location {
					name,
					event,
					r#type,
					region: data.regions.name(&region).unwrap(),
					rules,
				});
			}));
		},
		"misc_items" => {
			reader.sheet(worksheet!(|item: i32,
			                         id: String,
			                         r#type: ItemType,
			                         count: usize,
			                         class: ItemClass,
			                         name: String,
			                         groups: Vec<String>| {
				if let ItemType::Level(level) = r#type {
					data.levels[level].unlock = ItemIndex(item);
				}
				data.items.set(Some(id.clone()), ItemIndex(item), Item {
					id,
					name,
					count,
					class,
					r#type,
					groups,
				});
				data.sorted_items.push(ItemIndex(item));
			}));
		},
		"trash_types" => {
			reader.sheet(worksheet!(|item: i32,
			                         location: i32,
			                         extra: Option<TrashTypeExtra>,
			                         id: String,
			                         class: ItemClass,
			                         name: String| {
				let item_id = format!("Trash{id}");
				let name = format!("Trash: {name}");
				let region = data.regions.add_sequential(Some(item_id.clone()), Region {
					name: name.clone(),
					next: None,
					connections: BTreeMap::new(),
					position: BTreeSet::new(),
				});
				data.locations.set(Some(item_id.clone()), LocationIndex(location), Location {
					name: name.clone(),
					event: matches!(extra, Some(TrashTypeExtra::Event)).then(|| item_id.clone()),
					r#type: LocationType::TrashType,
					region,
					rules: Rule::True,
				});
				data.items.set(Some(item_id.clone()), ItemIndex(item), Item {
					id: item_id,
					name,
					count: 1,
					class,
					r#type: ItemType::TrashType,
					groups: vec!["Trash".to_owned()],
				});
				data.sorted_items.push(ItemIndex(item));
				if matches!(extra, Some(TrashTypeExtra::Start)) {
					data.start_trash.push(ItemIndex(item));
				}
			}))
		},
		"trashsanity" => {
			let mut current_scene = String::new();
			let mut current_index = 0;
			reader.sheet(worksheet!(|id: Option<i32>,
			                         scene: Option<String>,
			                         index: usize,
			                         name: String,
			                         parent: Option<usize>,
			                         visual: Option<String>,
			                         r#type: Option<String>,
			                         region: Option<String>,
			                         dependant: Option<String>,
			                         rules: Rule| {
				if let Some(scene) = scene {
					current_scene = scene.clone();
					assert!(
						data.scenes
							.insert(scene.clone(), Scene { name: scene.clone(), trash: Vec::new() })
							.is_none(),
						"duplicate scene {scene:?}"
					);
					assert_eq!(index, 0, "bad start of scene {scene:?}");
					current_index = 0;
				} else {
					current_index += 1;
					assert_eq!(index, current_index, "bad scene increment {current_scene:?}");
				}
				assert_eq!(
					data.scenes[&current_scene].trash.len(),
					current_index,
					"missed push somewhere?"
				);
				if let Some(parent) = parent {
					assert!(
						id.is_none(),
						"invalid parent entry {current_scene:?} {current_index:?}"
					);
					assert!(
						visual.is_none(),
						"invalid parent entry {current_scene:?} {current_index:?}"
					);
					assert!(
						region.is_none(),
						"invalid parent entry {current_scene:?} {current_index:?}"
					);
					assert!(
						dependant.is_none(),
						"invalid parent entry {current_scene:?} {current_index:?}"
					);
					assert_eq!(
						rules,
						Rule::True,
						"invalid parent entry {current_scene:?} {current_index:?}"
					);
					let parent = data.scenes[&current_scene].trash[parent].clone();
					data.scenes.get_mut(&current_scene).unwrap().trash.push(Trash {
						name,
						r#type: parent.r#type,
						location: parent.location,
					});
				} else if let (Some(id), Some(visual), Some(r#type), Some(region)) =
					(id, visual.clone(), r#type.clone(), &region)
				{
					let trash_name = format!("Trash{type}");
					let rules = Rule::And(vec![Rule::Trash(trash_name.clone()), rules.clone()]);
					// considered using CanReachLocation rule, but it wouldn't deduplicate under And
					if let Some(dependant) = dependant {
						if let Some(location) = data.locations.name(&dependant) {
							let location = &mut data.locations[location];
							location.rules.and_trash(rules.clone());
						}
						let region = data.regions.name(&dependant).unwrap();
						if let Some(next) = &mut data.regions[region].next {
							next.rules.and_trash(rules.clone());
						}
					}
					let region = data.regions.name(region).unwrap();
					let trash_region = data.regions.name(&trash_name).unwrap();
					let trash_entrance_name = format!(
						"{} in {}",
						data.items.get_named(&trash_name).name,
						data.regions[region].name
					);
					data.regions[region]
						.connections
						.entry(trash_region)
						.or_insert_with(|| RegionConnection {
							to: trash_region,
							name: trash_entrance_name,
							rules: Rule::Or(Vec::new()),
						})
						.rules
						.or_trash(rules.clone());
					let local_positions = data.regions[region].position.clone();
					data.regions[trash_region].position.extend(local_positions);
					data.locations.set(None, LocationIndex(id), Location {
						name: format!("{}: {visual}", data.regions[region].name),
						// TODO: having locations use level name (and only differentiating by region when needed) is preferred
						//name: format!("{}: {visual}", data.levels[data.regions[region].position.first().unwrap().0].name),
						event: None,
						r#type: LocationType::Trash,
						region,
						rules,
					});
					data.scenes.get_mut(&current_scene).unwrap().trash.push(Trash {
						name,
						r#type: Some(r#type),
						location: Some(LocationIndex(id)),
					});
				} else {
					assert!(
						id.is_none(),
						"invalid typeonly entry {current_scene:?} {current_index:?}"
					);
					assert!(
						visual.is_none(),
						"invalid typeonly entry {current_scene:?} {current_index:?}"
					);
					assert!(
						region.is_none(),
						"invalid typeonly entry {current_scene:?} {current_index:?}"
					);
					assert!(
						dependant.is_none(),
						"invalid typeonly entry {current_scene:?} {current_index:?}"
					);
					assert_eq!(
						rules,
						Rule::True,
						"invalid typeonly entry {current_scene:?} {current_index:?}"
					);
					data.scenes.get_mut(&current_scene).unwrap().trash.push(Trash {
						name,
						r#type,
						location: None,
					});
				}
			}))
		},
		name => println!("ignoring worksheet {name:?}"),
	});
	data.fill_sorted_locations();
	println!("location gaps: {:?}", data.locations.find_gaps().debug());
	println!("item gaps: {:?}", data.items.find_gaps().debug());
	let mut trash_type_map = BTreeMap::<&str, usize>::new();
	for r#type in data
		.scenes
		.values()
		.flat_map(|scene| scene.trash.iter())
		.filter_map(|trash| trash.r#type.as_deref())
	{
		*trash_type_map.entry(r#type).or_default() += 1;
	}
	println!("trash types: {trash_type_map:?}");
	serde_json::to_writer(
		File::create("../donutcounty/archipelago.json").unwrap(),
		&PyArchipelago::new(&data),
	);
	serde_json::to_writer(File::create("../donutcounty/logic.json").unwrap(), &PyData::new(&data));
	serde_json::to_writer(File::create("../Generated/logic.json").unwrap(), &GameData::new(&data));
	write!(File::create("../Generated/ItemId.cs").unwrap(), "{}", GameDataItemIds::new(&data))
		.unwrap();
	write!(
		File::create("../Generated/VersionInfo.cs").unwrap(),
		"{}",
		GameDataVersionInfo::new(&data)
	)
	.unwrap();
	let csproj_version = Version::from_cell(&SheetReader::read_csproj("../DonutCountyAP.csproj"));
	if csproj_version != data.version {
		println!(
			"!!!!! csproj mismatch, should be {}, got {} !!!!!",
			data.version.string(),
			csproj_version.string()
		);
	}
}
