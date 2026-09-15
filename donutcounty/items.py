from __future__ import annotations

from collections.abc import Callable
from dataclasses import dataclass
from typing import TYPE_CHECKING, Optional

from BaseClasses import Item, ItemClassification
from rule_builder.rules import OptionFilter

from . import logic
from .options import GoalArea

if TYPE_CHECKING:
    from .world import DonutCountyWorld
    
class DonutCountyItem(Item):
    game = "Donut County"

def get_random_filler_item_name(world: DonutCountyWorld) -> str:
    return world.random.choice(logic.fillers[world.random.choices(world.dc_gen_data["filler_lists"][0], world.dc_gen_data["filler_lists"][1])[0]])

def create_item(world: DonutCountyWorld, name: str) -> DonutCountyItem:
    item_class = ItemClassification(logic.item_to_class[name])
    if item_class == -1: # BossfightProgression
        item_class = ItemClassification.progression
        if world.options.goal_area == GoalArea.option_bossfight:
            item_class |= ItemClassification.useful
    return DonutCountyItem(name, item_class, logic.item_to_id[name], world.player)

def create_nonprogression_piece(world: DonutCountyWorld) -> DonutCountyItem:
    item = world.create_item(logic.piece)
    item.classification = ItemClassification.useful
    return item

def roll_required_pieces(world: DonutCountyWorld, total: int) -> tuple[list[int], int]:
    prp = world.options.pieces_required_percent.value
    def total_percent(n: int, d: int) -> int:
        return (total * prp * n + 100 * d - 1) // (100 * d)
    out = [0] * 22
    #starting_level = world.random.choice([12, 19])
    ending_level = 21 if world.options.goal_area == GoalArea.option_aftermath else 20
    #if world.options.pieces_unlock_levels:
    #    other_levels = list(set(range(21)).difference({starting_level, ending_level}))
    #    world.random.shuffle(other_levels)
    #    level_order = [starting_level] + other_levels
    #    for i, level in enumerate(level_order):
    #        out[level] = total_percent(i, len(level_order))
    out[ending_level] = total_percent(1, 1)
    return out, out[ending_level]

def create_all_items(world: DonutCountyWorld) -> None:
    is_ut_gen = "UT" in world.dc_gen_data
    total_locations = len(world.multiworld.get_unfilled_locations(world.player))
    itempool: list[Item] = []
    sifp: dict[str, int] = {}
    def add_sifp(item: str):
        nonlocal sifp
        sifp[item] = 1
        world.multiworld.push_precollected(world.create_item(item))
    if world.options.levels:
        # levels with two or more checks to prevent restrictive starts
        add_sifp(logic.level_items[world.random.choice([1, 2, 12, 19])])
    if world.options.trashsanity:
        add_sifp("Rock")
        add_sifp("Grass")
        add_sifp("Brick")
        add_sifp("Donut")
    if is_ut_gen:
        # just add way too many items to the pool anyways, might be unneeded
        sifp = {}
    for item_group in logic.items:
        condition = item_group[0]
        items = item_group[1]
        if condition is None or OptionFilter.from_dict(condition).check(world.options):
            for item in items:
                name = item[0]
                count = item[1]
                if name in sifp:
                    count = max(0, count - sifp[name])
                itempool += [world.create_item(name) for _ in range(count)]
    
    if is_ut_gen:
        spawn_pieces = world.dc_slot_data["total_pieces"]
        required_for_goal = max(world.dc_slot_data["required_pieces"])
        # force all UT pieces progression so /next_progression can see them
        itempool += [world.create_item(logic.piece) for _ in range(spawn_pieces)]
    else:
        unfilled_after_basic = total_locations - len(itempool)
        spawn_pieces = min(unfilled_after_basic, world.options.total_pieces.value)
        assert spawn_pieces >= 0, "Not enough item space to place any quadcopter pieces"
        world.dc_slot_data["total_pieces"] = spawn_pieces
        world.dc_slot_data["required_pieces"], required_for_goal = roll_required_pieces(world, spawn_pieces)
        itempool += [world.create_item(logic.piece) for _ in range(required_for_goal)]
        itempool += [create_nonprogression_piece(world) for _ in range(spawn_pieces - required_for_goal)]
    
    # UT gens might have more items than locations
    unfilled = max(0, total_locations - len(itempool))
    itempool += [world.create_filler() for _ in range(unfilled)]
    world.multiworld.itempool += itempool
    