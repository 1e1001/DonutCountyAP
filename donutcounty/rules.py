from __future__ import annotations

from typing import TYPE_CHECKING

from rule_builder.rules import Has, True_

from . import autologic

if TYPE_CHECKING:
    from .world import DonutCountyWorld

def set_all_rules(world: DonutCountyWorld) -> None:
    world.set_completion_rule(Has("Victory"))
    # we don't know fragment count until items are added, so this needs to be delayed
    for level, count in zip(autologic.LEVEL_ENTRANCES, world.dc_slot_data["required_fragments"]):
        rules = Has("Quadcopter Piece", count)
        rules = (autologic.HasFlag(level[1]) & rules) if level[1] is not None else rules
        world.set_rule(world.get_entrance("Enter " + level[0]), rules)