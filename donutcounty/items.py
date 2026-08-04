from __future__ import annotations

from collections.abc import Callable
from dataclasses import dataclass
from typing import TYPE_CHECKING, Optional

from BaseClasses import Item, ItemClassification
from rule_builder.options import OptionFilter
from rule_builder.rules import Has

from .options import Water, Fire, Snake, Light, Bunnies, Catapult, SnakeDanger, SaltAndPepper, HackProtocol

if TYPE_CHECKING:
    from .world import DonutCountyWorld
    
class DonutCountyItem(Item):
    game = "Donut County"

@dataclass
class RawItem():
    id: int
    name: str
    kind: ItemClassification
    enabled: Optional[OptionFilter]

_item_id = 1
ITEMS: dict[str, RawItem] = {}
BASIC_ITEMS: list[RawItem] = []

def raw_item(item: RawItem):
    global _item_id
    item.id = _item_id
    _item_id += 1
    ITEMS[item.name] = item

def basic_item(count: int, name: str, kind: ItemClassification, enabled: Optional[OptionFilter]):
    global BASIC_ITEMS
    item = RawItem(0, name, kind, enabled)
    raw_item(item)
    BASIC_ITEMS += [item for _ in range(count)]

def non_item(count: int):
    global _item_id
    _item_id += count

# todo: give these better names
prog_use = ItemClassification.progression | ItemClassification.useful
_item_id = 1000
basic_item(1, "Hole: Water", prog_use, OptionFilter(Water, Water.option_true))
basic_item(1, "Hole: Fire", prog_use, OptionFilter(Fire, Fire.option_true))
basic_item(1, "Hole: Snake", prog_use, OptionFilter(Snake, Snake.option_true))
basic_item(1, "Hole: Light", prog_use, OptionFilter(Light, Light.option_true))
basic_item(1, "Hole: Bunnies", prog_use, OptionFilter(Bunnies, Bunnies.option_true))
_item_id = 2000
basic_item(1, "Catapult", prog_use, OptionFilter(Catapult, Catapult.option_global))
split_catapult = OptionFilter(Catapult, Catapult.option_individual)
basic_item(1, "Catapult: Boxes", prog_use, split_catapult)
basic_item(1, "Catapult: Chickens", prog_use, split_catapult)
basic_item(1, "Catapult: Eggs", prog_use, split_catapult)
basic_item(1, "Catapult: Honeycomb", prog_use, split_catapult)
basic_item(1, "Catapult: Frogs", prog_use, split_catapult)
basic_item(1, "Catapult: Water Balloons", prog_use, split_catapult)
basic_item(1, "Catapult: Water", prog_use, split_catapult)
basic_item(1, "Catapult: Donuts, Cameras, and Raccoons", prog_use, split_catapult)
basic_item(1, "Catapult: Hacking Device", prog_use, split_catapult)
basic_item(1, "Catapult: Bombs", prog_use, split_catapult)
_item_id = 3000
raw_item(RawItem(0, "Fragment", ItemClassification.progression, None))
raw_item(RawItem(0, "BK Does One (1) Backflip", ItemClassification.filler, None))
raw_item(RawItem(0, "Concrete Trap", ItemClassification.trap, None))
raw_item(RawItem(0, "999ft Below Trap", ItemClassification.trap, None))
_item_id = 4000
non_item(21) # deliveries
_item_id = 5000
non_item(58) # segments
_item_id = 6000
basic_item(4, "Progressive Snake Danger", ItemClassification.progression, OptionFilter(SnakeDanger, SnakeDanger.option_true))
non_item(3) # snake danger locations
basic_item(2, "Progressive Salt", ItemClassification.progression, OptionFilter(SaltAndPepper, SaltAndPepper.option_true))
non_item(1) # salt
basic_item(3, "Progressive Pepper", ItemClassification.progression, OptionFilter(SaltAndPepper, SaltAndPepper.option_true))
non_item(2) # pepper
basic_item(1, "H.A.C.K. Protocol", ItemClassification.progression, OptionFilter(HackProtocol, HackProtocol.option_true))
_item_id = 7000
non_item(16) # achievements

def HasBasic(name: str, amount: int = 1):
    item = ITEMS[name]
    return Has(name, amount, options=[item.enabled] if item.enabled else [], filtered_resolution=True)

def HasHole(name: str):
    return HasBasic("Hole: " + name)

def HasCatapult(name: str):
    return HasBasic("Catapult") & HasBasic("Catapult: " + name)

ITEM_NAME_TO_ID = { name: item.id for name, item in ITEMS.items() }

def get_random_filler_item_name(world: DonutCountyWorld) -> str:
    # TODO: weighted roll
    return "BK Does One (1) Backflip"


def create_item(world: DonutCountyWorld, name: str) -> DonutCountyItem:
    item = ITEMS[name]
    return DonutCountyItem(name, item.kind, item.id, world.player)


def create_all_items(world: DonutCountyWorld) -> None:
    total_locations = len(world.multiworld.get_unfilled_locations(world.player))
    itempool: list[Item] = []
    for item in BASIC_ITEMS:
        if item.enabled.check(world.options):
            itempool.append(world.create_item(item.name))
    unfilled_after_basic = total_locations - len(itempool)
    spawn_fragments = min(unfilled_after_basic, world.options.total_fragments.value)
    assert spawn_fragments >= 0, "Not enough item space for fragments"
    itempool += [world.create_item("Fragment") for _ in range(spawn_fragments)]
    required_fragments = (spawn_fragments * world.options.fragments_required_percent.value + 99) // 100
    world.dc_total_fragments = spawn_fragments
    world.dc_required_fragments = required_fragments
    unfilled = total_locations - len(itempool)
    itempool += [world.create_filler() for _ in range(unfilled)]
    world.multiworld.itempool += itempool