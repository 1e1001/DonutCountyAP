from __future__ import annotations

from collections.abc import Callable
from dataclasses import dataclass
from typing import TYPE_CHECKING, Optional

from BaseClasses import Item, ItemClassification
from rule_builder.rules import Has

from . import autologic
from .options import GoalArea

if TYPE_CHECKING:
    from .world import DonutCountyWorld
    
class DonutCountyItem(Item):
    game = "Donut County"

def get_random_filler_item_name(world: DonutCountyWorld) -> str:
    if "filler_lists" not in world.dc_gen_data:
        filler_lists = (list(world.options.filler_weights.value.keys()), list(world.options.filler_weights.value.values()))
        if sum(filler_lists[1]) == 0:
            filler_lists[0].append("filler")
            filler_lists[1].append(1)
        world.dc_gen_data["filler_lists"] = filler_lists
    return autologic.ITEM_FILLER[world.random.choices(world.dc_gen_data["filler_lists"][0], world.dc_gen_data["filler_lists"][1])[0]]

def create_item(world: DonutCountyWorld, name: str) -> DonutCountyItem:
    return DonutCountyItem(name, autologic.ITEM_DATA[name][0], autologic.ITEM_NAME_TO_ID[name], world.player)

def create_all_items(world: DonutCountyWorld) -> None:
    total_locations = len(world.multiworld.get_unfilled_locations(world.player))
    itempool: list[Item] = []
    def for_item(quantity, name):
        nonlocal itempool
        itempool += [world.create_item(name) for _ in range(quantity)]
    autologic.items(world.options, for_item)
    unfilled_after_basic = total_locations - len(itempool)
    # TODO: load data from dc_gen_data for universal tracker
    spawn_fragments = min(unfilled_after_basic, world.options.total_fragments.value)
    assert spawn_fragments >= 0, "Not enough item space for fragments"
    itempool += [world.create_item("Quadcopter Piece") for _ in range(spawn_fragments)]
    required_fragments = (spawn_fragments * world.options.fragments_required_percent.value + 99) // 100
    world.dc_slot_data["total_fragments"] = spawn_fragments
    # TODO: per-level fragment rando - clean this up
    world.dc_slot_data["required_fragments"] = [0] * 20 + [required_fragments, 0]
    if world.options.goal_area == GoalArea.option_aftermath:
        world.dc_slot_data["required_fragments"][21] = world.dc_slot_data["required_fragments"][20]
        world.dc_slot_data["required_fragments"][20] = 0
    unfilled = total_locations - len(itempool)
    itempool += [world.create_filler() for _ in range(unfilled)]
    world.multiworld.itempool += itempool