from __future__ import annotations

from collections.abc import Callable
from dataclasses import dataclass
from typing import TYPE_CHECKING, Optional

from BaseClasses import Item, ItemClassification

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

def create_nonprogression_piece(world: DonutCountyWorld) -> DonutCountyItem:
    item = world.create_item("Quadcopter Piece")
    item.classification = ItemClassification.useful
    return item

def roll_required_pieces(world: DonutCountyWorld, total: int) -> tuple[list[int], int]:
    prp = world.options.pieces_required_percent.value
    def total_percent(n: int, d: int) -> int:
        return (total * prp * n + 100 * d - 1) // (100 * d)
    out = [0] * 22
    # TODO: this shuffle logic is kinda incomplete
    starting_level = world.random.choice([12, 19])
    ending_level = 21 if world.options.goal_area == GoalArea.option_aftermath else 20
    if world.options.pieces_unlock_levels:
        other_levels = list(set(range(21)).difference({starting_level, ending_level}))
        world.random.shuffle(other_levels)
        level_order = [starting_level] + other_levels
        for i, level in enumerate(level_order):
            out[level] = total_percent(i, len(level_order))
    out[ending_level] = total_percent(1, 1)
    return out, out[ending_level]

def create_all_items(world: DonutCountyWorld) -> None:
    total_locations = len(world.multiworld.get_unfilled_locations(world.player))
    itempool: list[Item] = []
    def for_item(quantity, name):
        nonlocal itempool
        # not sure how si-as-sifp would work with UT support so i'm conservatively removing it for now
        ## si-as-sifp lets extra space be used for pieces
        #if quantity == 1 and name in world.options.start_inventory:
        #    return
        itempool += [world.create_item(name) for _ in range(quantity)]
    autologic.items(world.options, for_item)
    
    if "ut" in world.dc_gen_data:
        spawn_pieces = world.dc_slot_data["total_pieces"]
        required_for_goal = max(world.dc_slot_data["required_pieces"])
    else:
        unfilled_after_basic = total_locations - len(itempool)
        spawn_pieces = min(unfilled_after_basic, world.options.total_pieces.value)
        assert spawn_pieces >= 0, "Not enough item space to place any quadcopter pieces"
        world.dc_slot_data["total_pieces"] = spawn_pieces
        world.dc_slot_data["required_pieces"], required_for_goal = roll_required_pieces(world, spawn_pieces)
    itempool += [world.create_item("Quadcopter Piece") for _ in range(required_for_goal)]
    itempool += [create_nonprogression_piece(world) for _ in range(spawn_pieces - required_for_goal)]
    
    unfilled = total_locations - len(itempool)
    itempool += [world.create_filler() for _ in range(unfilled)]
    world.multiworld.itempool += itempool