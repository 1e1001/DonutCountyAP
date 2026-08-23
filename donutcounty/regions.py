from __future__ import annotations

from typing import TYPE_CHECKING

from BaseClasses import Region

from .options import GoalArea

from . import autologic

if TYPE_CHECKING:
    from .world import DonutCountyWorld

def create_and_connect_regions(world: DonutCountyWorld) -> None:
    def world_region(name, parent, rules):
        region = Region(name, world.player, world.multiworld)
        world.multiworld.regions.append(region)
        world.get_region(parent).connect(region, ("Start " + name) if parent == "Menu" else ("Complete " + parent), rules)
    menu = Region("Menu", world.player, world.multiworld)
    aftermath = Region("Aftermath0", world.player, world.multiworld)
    texting = Region("Texting", world.player, world.multiworld)
    world.multiworld.regions += [menu, aftermath, texting]
    autologic.regions(world_region)
    world.get_region("MirasHouse0").connect(texting, "Texting MirasHouse")
    world.get_region("GeckoPark0").connect(texting, "Texting GeckoPark")
    world.get_region("ChickenBarn1").connect(texting, "Texting ChickenBarn")
    world.get_region("RaccoonHQ0").connect(texting, "Texting RaccoonHQ")
    aftermath_parent = world.get_region("BossFight3") if world.options.goal_area == GoalArea.option_bossfight else menu
    aftermath_parent.connect(aftermath, "Start Aftermath0")

    
    