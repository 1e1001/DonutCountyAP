from __future__ import annotations

from dataclasses import dataclass
from typing import TYPE_CHECKING, Callable, Optional

from BaseClasses import ItemClassification, Location
from rule_builder.rules import Rule, True_

from . import items

if TYPE_CHECKING:
    from .world import DonutCountyWorld
    
class DonutCountyLocation(Location):
    game = "Donut County"

@dataclass
class RawLoc():
    id: int
    name: str
    region: str
    enabled: Optional[Callable]
    rule: Rule
    # TODO: add logic rules

_loc_id = 1
LOCS: dict[str, RawLoc] = {}

def raw_loc(loc: RawLoc):
    global _loc_id
    loc.id = _loc_id
    _loc_id += 1
    LOCS[loc.name] = loc

def basic_loc(name: str, region: str, enabled: Callable, rule: Rule):
    raw_loc(RawLoc(0, name, region, enabled, rule))

def non_loc(count: int):
    global _loc_id
    _loc_id += count

# TODO: location-specific logic (e.g. achievements)
_loc_id = 1000
non_loc(5) # hole abilities
_loc_id = 2000
basic_loc("Buy Catapult", "ChickenBarn1", lambda world: world.options.buy_catapult, True_())
non_loc(10) # split catapult
_loc_id = 3000
non_loc(4) # fragment, filler, traps
#has_delivery = lambda world: world.options.level_completions
_loc_id = 4000
has_delivery = None
basic_loc("Mira's House", "MirasHouse4", has_delivery, True_())
basic_loc("Potter's Rock", "PottersRock6", has_delivery, True_())
basic_loc("Ranger Station", "RangerStation4", has_delivery, True_())
basic_loc("Riverbed", "Riverbed2", has_delivery, True_())
basic_loc("Campground", "Campground3", has_delivery, True_())
basic_loc("Hopper Springs", "HopperSprings5", has_delivery, True_())
basic_loc("Joshua Tree", "JoshuaTree2", has_delivery, True_())
basic_loc("Beach Lot C", "BeachLotC4", has_delivery, True_())
basic_loc("Gecko Park", "GeckoPark4", has_delivery, True_())
basic_loc("Chicken Barn", "ChickenBarn6", has_delivery, True_())
basic_loc("Honey Nut Forest", "HoneyNutForest3", has_delivery, True_())
basic_loc("Cat Soup", "CatSoup5", has_delivery, True_())
basic_loc("Donut Shop", "DonutShop3", has_delivery, True_())
basic_loc("Abandoned House", "AbandonedHouse3", has_delivery, True_())
basic_loc("Raccoon Lagoon", "RaccoonLagoon5", has_delivery, True_())
basic_loc("The 405", "The4055", has_delivery, True_())
basic_loc("Raccoon HQ", "RaccoonHQ4", has_delivery, True_())
basic_loc("Biology Lab", "BiologyLab4", has_delivery, True_())
basic_loc("Anthropology Lab", "AnthropologyLab4", has_delivery, True_())
basic_loc("Trash King's Office", "TrashKingsOffice4", has_delivery, True_())
basic_loc("Boss Fight", "BossFight3", has_delivery, True_())
_loc_id = 5000
has_segments = lambda world: world.options.level_segments
basic_loc("Mira's House: Donut County", "MirasHouse1", has_segments, True_())
basic_loc("Potter's Rock: Pile", "PottersRock1", has_segments, True_())
basic_loc("Potter's Rock: Front", "PottersRock2", has_segments, True_())
basic_loc("Potter's Rock: Potter", "PottersRock3", has_segments, True_())
basic_loc("Potter's Rock: Pots", "PottersRock4", has_segments, True_())
basic_loc("Potter's Rock: Launch", "PottersRock5", has_segments, True_())
basic_loc("Ranger Station: Snakes", "RangerStation1", has_segments, True_())
basic_loc("Ranger Station: Snake hole", "RangerStation2", has_segments, True_())
basic_loc("Ranger Station: Snake danger", "RangerStation3", has_segments, True_())
basic_loc("Riverbed: Small Dog", "Riverbed1", has_segments, True_())
basic_loc("Campground: Popcorn", "Campground1", has_segments, True_())
basic_loc("Campground: Right tent", "Campground2", has_segments, True_())
basic_loc("Hopper Springs: Carrots", "HopperSprings1", has_segments, True_())
basic_loc("Hopper Springs: Bunnies round 1", "HopperSprings2", has_segments, True_())
basic_loc("Hopper Springs: Bunnies round 2", "HopperSprings3", has_segments, True_())
basic_loc("Hopper Springs: Bunnies round 3", "HopperSprings4", has_segments, True_())
basic_loc("Joshua Tree: Arson", "JoshuaTree1", has_segments, True_())
basic_loc("Beach Lot C: First firework", "BeachLotC1", has_segments, True_())
basic_loc("Beach Lot C: Birds", "BeachLotC2", has_segments, True_())
basic_loc("Beach Lot C: Cliff", "BeachLotC3", has_segments, True_())
basic_loc("Gecko Park: Geckos", "GeckoPark1", has_segments, True_())
basic_loc("Gecko Park: Coco", "GeckoPark2", has_segments, True_())
basic_loc("Chicken Barn: Boxes", "ChickenBarn1", has_segments, True_())
basic_loc("Chicken Barn: BK's apartment", "ChickenBarn2", has_segments, True_())
basic_loc("Chicken Barn: Catapult tutorial", "ChickenBarn3", has_segments, True_())
basic_loc("Chicken Barn: Chicken toss", "ChickenBarn4", has_segments, True_())
basic_loc("Chicken Barn: Egg toss", "ChickenBarn5", has_segments, True_())
basic_loc("Honey Nut Forest: Get frog", "HoneyNutForest1", has_segments, True_())
basic_loc("Honey Nut Forest: Frog tutorail", "HoneyNutForest2", has_segments, True_())
basic_loc("Cat Soup: Outside", "CatSoup1", has_segments, True_())
basic_loc("Cat Soup: Cooking", "CatSoup2", has_segments, True_())
basic_loc("Cat Soup: Dining", "CatSoup3", has_segments, True_())
basic_loc("Cat Soup: Cars", "CatSoup4", has_segments, True_())
basic_loc("Donut Shop: Car", "DonutShop1", has_segments, True_())
basic_loc("Abandoned House: Dark", "AbandonedHouse1", has_segments, True_())
basic_loc("Abandoned House: Light", "AbandonedHouse2", has_segments, True_())
basic_loc("Raccoon Lagoon: Water wheel", "RaccoonLagoon1", has_segments, True_())
basic_loc("Raccoon Lagoon: Log flume", "RaccoonLagoon2", has_segments, True_())
basic_loc("Raccoon Lagoon: Dispenser", "RaccoonLagoon3", has_segments, True_())
basic_loc("Raccoon Lagoon: Ferris wheel", "RaccoonLagoon4", has_segments, True_())
basic_loc("The 405: Small car", "The4051", has_segments, True_())
basic_loc("The 405: Truck repair", "The4052", has_segments, True_())
basic_loc("The 405: Cop raccoons", "The4053", has_segments, True_())
basic_loc("The 405: Big boy", "The4054", has_segments, True_())
basic_loc("Raccoon HQ: Exterior", "RaccoonHQ1", has_segments, True_())
basic_loc("Raccoon HQ: USB drive", "RaccoonHQ2", has_segments, True_())
basic_loc("Raccoon HQ: Hacking complete", "RaccoonHQ3", has_segments, True_())
basic_loc("Biology Lab: Raccoons", "BiologyLab1", has_segments, True_())
basic_loc("Biology Lab: Snake", "BiologyLab2", has_segments, True_())
basic_loc("Biology Lab: Lab", "BiologyLab3", has_segments, True_())
basic_loc("Anthropology Lab: Garbage bin", "AnthropologyLab1", has_segments, True_())
basic_loc("Anthropology Lab: Water leak", "AnthropologyLab2", has_segments, True_())
basic_loc("Anthropology Lab: Firework", "AnthropologyLab3", has_segments, True_())
basic_loc("Trash King's Office: First donuts", "TrashKingsOffice1", has_segments, True_())
basic_loc("Trash King's Office: Second donuts", "TrashKingsOffice2", has_segments, True_())
basic_loc("Trash King's Office: Third donuts", "TrashKingsOffice3", has_segments, True_())
basic_loc("Boss Fight: Phase 1", "BossFight1", has_segments, True_())
basic_loc("Boss Fight: Phase 2", "BossFight2", has_segments, True_())
_loc_id = 6000
has_snake_danger = lambda world: world.options.snake_danger
basic_loc("Snake Danger: Snake", "RangerStation2", has_snake_danger, True_())
basic_loc("Snake Danger: Horn", "RangerStation2", has_snake_danger, True_())
basic_loc("Snake Danger: Sign", "RangerStation2", has_snake_danger, True_())
basic_loc("Snake Danger: Swing", "RangerStation2", has_snake_danger, True_())
has_salt_and_pepper = lambda world: world.options.salt_and_pepper
basic_loc("Salt 1", "CatSoup1", has_salt_and_pepper, True_())
basic_loc("Salt 2", "CatSoup1", has_salt_and_pepper, True_())
basic_loc("Pepper 1", "CatSoup1", has_salt_and_pepper, True_())
basic_loc("Pepper 2", "CatSoup1", has_salt_and_pepper, True_())
basic_loc("Pepper 3", "CatSoup1", has_salt_and_pepper, True_())
basic_loc("H.A.C.K. Protocol", "RaccoonHQ2", lambda world: world.options.hack_protocol, True_())
_loc_id = 7000
has_achievements = lambda world: world.options.achievements
basic_loc("Bandit", "BiologyLab4", has_achievements, True_())
basic_loc("Secret Soup", "CatSoup1", has_achievements, True_())
basic_loc("Game Over", "BossFight0", has_achievements, True_())
basic_loc("Disrespecter", "AnthropologyLab1", has_achievements, True_())
basic_loc("Quack Enthusiast", "Menu", has_achievements, True_())
basic_loc("Egg Breaker", "ChickenBarn4", has_achievements, True_())
basic_loc("Music Lover", "GeckoPark3", has_achievements, True_())
basic_loc("Flawless", "BossFight1", has_achievements, True_())
basic_loc("Dethroner", "BossFight1", has_achievements, True_())
basic_loc("Boss Fight", "BossFight1", has_achievements, True_())
basic_loc("Hacker", "RaccoonHQ3", has_achievements, True_())
basic_loc("The Flume Is Doomed", "RaccoonLagoon4", has_achievements, True_())
basic_loc("Donut County", "DonutShop2", has_achievements, True_())
basic_loc("Gamer", "ChickenBarn1", has_achievements, True_())
basic_loc("Pyro", "JoshuaTree0", has_achievements, True_())
basic_loc("Pup's Oddyssey", "PottersRock5", has_achievements, True_())


LOCATION_NAME_TO_ID = { name: loc.id for name, loc in LOCS.items() }

def create_all_locations(world: DonutCountyWorld) -> None:
    for loc in LOCS.values():
        if (loc.enabled is None) or loc.enabled(world):
            region = world.get_region(loc.region)
            location = DonutCountyLocation(world.player, loc.name, loc.id, region)
            world.set_rule(location, loc.rule)
            region.locations.append(location)
    world.get_region("Aftermath0").add_event("Aftermath", "Victory", location_type=DonutCountyLocation, item_type=items.DonutCountyItem)
