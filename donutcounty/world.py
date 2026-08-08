from collections.abc import Mapping
from typing import Any

from worlds.AutoWorld import World

from . import autologic, items, locations, regions, rules, web_world
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
    item_name_to_id = autologic.ITEM_NAME_TO_ID
    location_name_to_id = autologic.LOCATION_NAME_TO_ID
    # TODO: groups
    item_name_groups = {}
    location_name_groups = {}
    dc_gen_data = {}
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
        for k, v in self.options.as_dict("goal_area", "levels", "hole_water", "hole_fire", "hole_snake", "hole_light", "hole_bunnies", "catapult", "level_segments", "achievements", "buy_catapult", "snake_danger", "salt_and_pepper", "hack_protocol").items():
            self.dc_slot_data[k] = v
        self.dc_slot_data["level_completions"] = True
        return self.dc_slot_data
    def custom_ut_sort(self, region_label: str, location_label: str) -> str | int:
        return autologic.LOCATION_SORT_ORDER[location_label]