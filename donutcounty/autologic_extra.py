from __future__ import annotations

from typing import TYPE_CHECKING, Optional

from BaseClasses import ItemClassification
from rule_builder.options import OptionFilter
from rule_builder.rules import Has, True_

from . import options

if TYPE_CHECKING:
    from .world import DonutCountyWorld

ITEM_RULES: dict[str, Optional[OptionFilter]] = {}

def HasFlag(name: str, amount: int = 1):
    item = ITEM_RULES[name]
    return Has(name, amount, options=[item] if item else [], filtered_resolution=True)