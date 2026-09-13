from __future__ import annotations
from dataclasses import dataclass

from typing import TYPE_CHECKING
from typing_extensions import override

from rule_builder.rules import Rule, Has, OptionFilter

from . import logic

if TYPE_CHECKING:
    from .world import DonutCountyWorld

@dataclass()
class LevelPieces(Rule["DonutCountyWorld"], game="Donut County"):
    index: int
    @override
    def _instantiate(self, world: "DonutCountyWorld") -> Rule.Resolved:
        rule = Has(logic.piece, world.dc_slot_data["required_pieces"][self.index])
        level_item = logic.level_items[self.index]
        if world.options.levels and level_item is not None:
            rule &= Has(level_item)
        return rule.resolve(world)


def set_all_rules(world: DonutCountyWorld) -> None:
    world.set_completion_rule(Has("Victory"))
    for entrance in logic.entrances:
        name = entrance[2]
        rules = entrance[3]
        world.set_rule(world.get_entrance(name), world.rule_from_dict(rules))
    regions = [world.get_region(region) for region in logic.regions]
    for group, locations in logic.locations:
        if group is None or OptionFilter.from_dict(group).check(world.options):
            for location in locations:
                name = location[1]
                rules = location[3]
                world.set_rule(world.get_location(name), world.rule_from_dict(rules))
    
    #for level, count in zip(autologic.LEVEL_ENTRANCES, world.dc_slot_data["required_pieces"]):
    #    rules = Has("Quadcopter Piece", count) if count > 0 else True_()
    #    rules = (autologic.HasFlag(level[1]) & rules) if level[1] is not None else rules
    #    world.set_rule(world.get_entrance("Start " + level[0]), rules)
    