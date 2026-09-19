from collections.abc import Mapping
from typing import Any, Optional

from BaseClasses import MultiWorld
from Options import Option
from worlds.AutoWorld import World

from . import logic, items, locations, regions, rules, web_world
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
    item_name_to_id = logic.item_to_id
    location_name_to_id = logic.location_to_id
    item_name_groups = logic.item_groups
    location_name_groups = logic.location_groups
    def __init__(self, multiworld: MultiWorld, player: int):
        super().__init__(multiworld, player)
        self.dc_gen_data = {}
        self.dc_slot_data = {
            "version": logic.version,
        }
    def create_regions(self) -> None:
        regions.create_and_connect_regions(self)
        locations.create_all_locations(self)
    def set_rules(self) -> None:
        rules.set_all_rules(self)
        #from Utils import visualize_regions
        #state = self.multiworld.get_all_state()
        #state.update_reachable_regions(self.player)
        #reachable_regions = set(state.reachable_regions[self.player])
        #unreachable_regions = set()
        #for region in self.multiworld.regions:
        #    if region not in reachable_regions:
        #        unreachable_regions.add(region)
        #visualize_regions(root_region=self.get_region("Menu"), file_name="deez.puml", show_entrance_names = True, regions_to_highlight = unreachable_regions)
    def create_items(self) -> None:
        items.create_all_items(self)
    def create_item(self, name: str) -> items.DonutCountyItem:
        return items.create_item(self, name)
    def get_filler_item_name(self) -> str:
        return items.get_random_filler_item_name(self)
    def fill_slot_data(self) -> Mapping[str, Any]:
        # TODO: split direct options and generated values into sub-keys (how to deserialize in C#?)
        for k, v in self.options.as_dict("goal_area", "levels", "hole", "catapult", "texting", "trash_souls", "achievements", "snake_danger", "salt_and_pepper", "trashsanity").items():
            self.dc_slot_data[k] = v
        return self.dc_slot_data
    def custom_ut_sort(self, _region_label: str, location_label: str) -> str | int:
        return logic.location_to_sort[location_label]

    ut_can_gen_without_yaml = True
    glitches_item_name = "Glitches"
    @staticmethod
    def interpret_slot_data(slot_data: dict[str, Any]) -> dict[str, Any]:
        try:
            from worlds.tracker import TrackerException
        except ImportError:
            TrackerException = Exception
        parse_version = lambda text: [int(part) for part in str.split(text, ".")]
        str_version = lambda version: f"v{version[0]}.{version[1]}.{version[2]}-unstable.{version[3]}" if len(version) == 4 else f"v{version[0]}.{version[1]}.{version[2]}"
        client_version = parse_version(logic.version)
        compat_version = parse_version(logic.compat_version)
        server_version = parse_version(slot_data["version"])
        if len(server_version) == 4 and server_version != client_version:
            raise TrackerException(f"Server {str_version(server_version)} is unstable and different from client {str_version(client_version)}")
        if server_version < compat_version:
            raise TrackerException(f"Server {str_version(server_version)} is older than minimum supported {str_version(compat_version)} of client {str_version(client_version)}")
        if len(server_version) == 3 and len(client_version) == 4 and server_version >= client_version[:3]:
            raise TrackerException(f"Server {str_version(server_version)} is newer than client {str_version(client_version)}, please update your client")
        if server_version > client_version:
            raise TrackerException(f"Server {str_version(server_version)} is newer than client {str_version(client_version)}, please update your client")
        return slot_data
    def generate_early(self) -> None:
        # fix option conflict
        if self.options.trash_souls and not self.options.trashsanity:
            self.options.trash_souls.value = 0
        re_gen_passthrough = getattr(self.multiworld, "re_gen_passthrough", {})
        if re_gen_passthrough and self.game in re_gen_passthrough:
            self.dc_gen_data["UT"] = True
            slot_data: dict[str, Any] = re_gen_passthrough[self.game]
            for key, value in slot_data.items():
                if key in {"total_pieces", "required_pieces"}:
                    self.dc_slot_data[key] = value
                else:
                    opt: Optional[Option] = getattr(self.options, key, None)
                    if opt is not None:
                        setattr(self.options, key, opt.from_any(value))