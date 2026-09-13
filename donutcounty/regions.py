from __future__ import annotations

from typing import TYPE_CHECKING

from BaseClasses import Region

from .options import GoalArea

from . import logic

if TYPE_CHECKING:
    from .world import DonutCountyWorld

def create_and_connect_regions(world: DonutCountyWorld) -> None:
    regions = []
    for name in logic.regions:
        region = Region(name, world.player, world.multiworld)
        regions.append(region)
        world.multiworld.regions.append(region)
    for entrance in logic.entrances:
        region_from = entrance[0]
        region_to = entrance[1]
        name = entrance[2]
        if region_to == logic.aWin and world.options.goal_area == GoalArea.option_bossfight:
            region_from = logic.aCatapult
        regions[region_from].connect(regions[region_to], name)
