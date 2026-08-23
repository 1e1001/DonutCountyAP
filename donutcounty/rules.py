from __future__ import annotations

from typing import TYPE_CHECKING

from rule_builder.rules import Has, True_

from . import autologic

if TYPE_CHECKING:
    from .world import DonutCountyWorld

def set_all_rules(world: DonutCountyWorld) -> None:
    world.set_completion_rule(Has("Victory"))
    # we don't know piece count until items are added, so this needs to be delayed
    # TODO: consider delaying that even further, using an approach similar to what's described in
    # https://discord.com/channels/731205301247803413/1214608557077700720/1537987718875713607
    for level, count in zip(autologic.LEVEL_ENTRANCES, world.dc_slot_data["required_pieces"]):
        rules = Has("Quadcopter Piece", count) if count > 0 else True_()
        rules = (autologic.HasFlag(level[1]) & rules) if level[1] is not None else rules
        world.set_rule(world.get_entrance("Start " + level[0]), rules)