from __future__ import annotations

from typing import TYPE_CHECKING

from rule_builder.options import OptionFilter
from rule_builder.rules import Has, Rule

from .options import GoalArea

if TYPE_CHECKING:
    from .world import DonutCountyWorld

def set_all_rules(world: DonutCountyWorld) -> None:
    fragments_rule = Has("Fragment", world.dc_required_fragments)
    world.set_completion_rule(Has("Victory"))
    if world.options.goal_area == GoalArea.option_bossfight:
        world.set_rule(world.get_entrance("Enter BossFight0"), fragments_rule)
    else:
        world.set_rule(world.get_entrance("Enter Aftermath0"), fragments_rule)