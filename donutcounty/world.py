from collections.abc import Mapping
from typing import Any, Optional

from BaseClasses import MultiWorld
from Options import Option
from worlds.AutoWorld import World

from . import autologic, items, locations, regions, rules, web_world
from . import options as dc_options

# todo: try cached rule bulder world
class DonutCountyWorld(World):
    """
    Donut County is a physics puzzle game where you control an ever-growing hole in the ground.
    The randomizer makes each level available from the start, requiring certain ability items to progress further in each level.
    Completing levels and sections of levels sends items to other players.
    Either the Boss Fight or Aftermath level is locked behind gathering a number of `Quadcopter Piece` items.
    Once that is completed, you can enter Aftermath to win!
    Oh, and the raccoon's name is BK.
    """
    game = "Donut County"
    web = web_world.DonutCountyWebWorld()
    options_dataclass = dc_options.DonutCountyOptions
    options: dc_options.DonutCountyOptions
    item_name_to_id = autologic.ITEM_NAME_TO_ID
    location_name_to_id = autologic.LOCATION_NAME_TO_ID
    item_name_groups = autologic.ITEM_GROUPS
    location_name_groups = autologic.LOCATION_GROUPS
    def __init__(self, multiworld: MultiWorld, player: int):
        super().__init__(multiworld, player)
        self.dc_gen_data = {}
        self.dc_slot_data = {
            "version": "0.1.0",
        }
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
        # TODO: move options slot data into their own subkey (how to deserialize this?)
        for k, v in self.options.as_dict("goal_area", "levels", "hole", "catapult", "texting", "achievements", "buy_catapult", "snake_danger", "salt_and_pepper").items():
            self.dc_slot_data[k] = v
        return self.dc_slot_data
    def custom_ut_sort(self, region_label: str, location_label: str) -> str | int:
        return autologic.LOCATION_SORT_ORDER[location_label]

    ut_can_gen_without_yaml = True
    glitches_item_name = "Glitches"
    @staticmethod
    def interpret_slot_data(slot_data: dict[str, Any]) -> dict[str, Any]:
        return slot_data
    def generate_early(self) -> None:
        re_gen_passthrough = getattr(self.multiworld, "re_gen_passthrough", {})
        if re_gen_passthrough and self.game in re_gen_passthrough:
            self.dc_gen_data["ut"] = True
            slot_data: dict[str, Any] = re_gen_passthrough[self.game]
            for key, value in slot_data.items():
                if key in {"total_pieces", "required_pieces"}:
                    self.dc_slot_data[key] = value
                else:
                    opt: Optional[Option] = getattr(self.options, key, None)
                    if opt is not None:
                        setattr(self.options, key, opt.from_any(value))