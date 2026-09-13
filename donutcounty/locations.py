from __future__ import annotations

from typing import TYPE_CHECKING

from BaseClasses import Location
from rule_builder.rules import OptionFilter

from . import items, logic

if TYPE_CHECKING:
    from .world import DonutCountyWorld
    
class DonutCountyLocation(Location):
    game = "Donut County"

def create_all_locations(world: DonutCountyWorld) -> None:
    regions = [world.get_region(region) for region in logic.regions]
    for location_group in logic.locations:
        condition = location_group[0]
        locations = location_group[1]
        if condition is None or OptionFilter.from_dict(condition).check(world.options):
            for location in locations:
                location_id = location[0]
                name = location[1]
                region = regions[location[2]]
                location = DonutCountyLocation(world.player, name, location_id, region)
                region.locations.append(location)
    regions[logic.aWin].add_event("Aftermath", "Victory", location_type=DonutCountyLocation, item_type=items.DonutCountyItem)
