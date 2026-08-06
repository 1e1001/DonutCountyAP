from collections.abc import Mapping
from typing import Any

from worlds.AutoWorld import World

from . import items, locations, regions, rules, web_world
from . import options as dc_options

# todo: try cached rule bulder world
class DonutCountyWorld(World):
    """
    Donut County is a story-based physics puzzle game where you play as an ever-growing hole in the ground. Meet cute characters, steal their trash, and throw them in a hole.
    """
    game = "Donut County"
    web = web_world.DonutCountyWebWorld()
    options_dataclass = dc_options.DonutCountyOptions
    options: dc_options.DonutCountyOptions
    location_name_to_id = locations.LOCATION_NAME_TO_ID
    item_name_to_id = items.ITEM_NAME_TO_ID
    dc_slot_data = {}
    def create_regions(self) -> None:
        regions.create_and_connect_regions(self)
        locations.create_all_locations(self)
    def set_rules(self) -> None:
        rules.set_all_rules(self)
    def create_items(self) -> None:
        items.create_all_items(self)
    def create_item(self, name: str) -> items.DonutCountyItem:
        return items.create_item(self, name)
    def get_filler_item_name(self) -> str:
        return items.get_random_filler_item_name(self)
    def fill_slot_data(self) -> Mapping[str, Any]:
        for k, v in self.options.asdict("goal_area", "water", "fire", "snake", "light", "bunnies", "catapult", "level_completions", "level_segments", "achievements", "buy_catapult", "snake_danger", "salt_and_pepper", "hack_protocol").items():
            self.dc_slot_data[k] = v
        return self.dc_slot_data