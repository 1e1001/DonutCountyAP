from __future__ import annotations

from typing import TYPE_CHECKING

from BaseClasses import Region
from rule_builder.rules import True_

from .options import GoalArea

from . import autologic

if TYPE_CHECKING:
    from .world import DonutCountyWorld

def create_and_connect_regions(world: DonutCountyWorld) -> None:
    def world_region(name, parent, rules):
        region = Region(name, world.player, world.multiworld)
        world.multiworld.regions.append(region)
        world.get_region(parent).connect(region, "Enter " + name, rules)
    menu = Region("Menu", world.player, world.multiworld)
    aftermath = Region("Aftermath0", world.player, world.multiworld)
    world.multiworld.regions += [menu, aftermath]
    autologic.regions(world_region)
    aftermath_parent = world.get_region("BossFight3") if world.options.goal_area == GoalArea.option_bossfight else menu
    # this True_() is load-bearing for rules
    aftermath_parent.connect(aftermath, "Enter Aftermath0", True_())
    
    