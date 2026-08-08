from __future__ import annotations

from typing import TYPE_CHECKING

from BaseClasses import Location

from . import items, autologic

if TYPE_CHECKING:
    from .world import DonutCountyWorld
    
class DonutCountyLocation(Location):
    game = "Donut County"

def create_all_locations(world: DonutCountyWorld) -> None:
    def for_location(id_, name, region, rules):
        region = world.get_region(region)
        location = DonutCountyLocation(world.player, name, id_, region)
        world.set_rule(location, rules)
        region.locations.append(location)
    autologic.locations(world.options, for_location)
    world.get_region("Aftermath0").add_event("Aftermath", "Victory", location_type=DonutCountyLocation, item_type=items.DonutCountyItem)
