from __future__ import annotations

from typing import TYPE_CHECKING

from BaseClasses import Entrance, Region
from rule_builder.rules import Rule, True_, Has

from .options import GoalArea
from .items import DonutCountyItem, HasBasic, HasHole, HasCatapult

if TYPE_CHECKING:
    from .world import DonutCountyWorld

# TODO: progressive item logic
REGION_DEPTHS: dict[str, list[Rule]] = {
    "MirasHouse": [True_(), True_(), True_(), True_()],
    "PottersRock": [True_(), True_(), True_(), True_(), HasHole("Fire"), True_()],
    "RangerStation": [True_(), HasHole("Snake"), True_(), True_()],
    "Riverbed": [True_(), HasHole("Water")],
    "Campground": [HasHole("Fire"), True_(), True_()],
    "HopperSprings": [True_(), HasHole("Bunnies"), True_(), True_(), True_()],
    "JoshuaTree": [HasHole("Fire"), True_()],
    "BeachLotC": [HasHole("Fire"), True_(), True_(), True_()],
    "GeckoPark": [True_(), True_(), True_(), True_()],
    "ChickenBarn": [True_(), True_(), HasCatapult("Boxes"), HasCatapult("Chickens"), HasCatapult("Eggs"), True_()],
    "HoneyNutForest": [HasCatapult("Honeycomb"), HasCatapult("Frogs"), True_()],
    "CatSoup": [True_(), HasHole("Water"), True_(), True_(), True_()],
    "DonutShop": [True_(), True_(), True_()],
    "AbandonedHouse": [HasHole("Light"), True_(), True_()],
    "RaccoonLagoon": [HasHole("Water") & HasCatapult("Water Balloons") & HasCatapult("Water"), True_(), True_(), True_(), True_()],
    "The405": [True_(), True_(), HasCatapult("Donuts, Cameras, and Raccoons"), True_(), True_()],
    "RaccoonHQ": [True_(), True_(), HasCatapult("Hacking Device"), True_()],
    "BiologyLab": [True_(), HasHole("Snake"), HasCatapult("Frogs"), HasHole("Bunnies")],
    "AnthropologyLab": [True_(), HasHole("Fire"), HasHole("Water"), True_()],
    "TrashKingsOffice": [True_(), True_(), True_(), True_()],
    "BossFight": [HasCatapult("Bombs"), HasCatapult("Hacking Device"), True_()],
    "Aftermath": [],
}

def create_and_connect_regions(world: DonutCountyWorld) -> None:
    create_all_regions(world)
    connect_regions(world)
    
def create_all_regions(world: DonutCountyWorld) -> None:
    regions = [Region("Menu", world.player, world.multiworld)]
    for prefix, rules in REGION_DEPTHS.items():
        for i in range(len(rules) + 1):
            regions.append(Region(prefix + str(i), world.player, world.multiworld))
    world.multiworld.regions += regions
    
def connect_regions(world: DonutCountyWorld) -> None:
    menu = world.get_region("Menu")
    gate_aftermath = world.options.goal_area == GoalArea.option_bossfight
    for prefix, rules in REGION_DEPTHS.items():
        if not (prefix == "Aftermath" and gate_aftermath):
            menu.connect(world.get_region(prefix + "0"), "Enter " + prefix + "0")
        for i, rule in enumerate(rules):
            start = world.get_region(prefix + str(i))
            end = world.get_region(prefix + str(i + 1))
            start.connect(end, "Enter " + prefix + str(i + 1), rule)
    if gate_aftermath:
        world.get_region("BossFight3").connect(world.get_region("Aftermath0"), "Enter Aftermath0")
    