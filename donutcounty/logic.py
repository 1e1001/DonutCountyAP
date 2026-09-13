from __future__ import annotations

import pkgutil
from typing import Any
import orjson

logic = orjson.loads(pkgutil.get_data(__name__, "logic.json"))

# TODO: deserialize to dataclasses?
version: str = logic["version"]
compat_version: str = logic["compat_version"]
regions: list[str] = logic["regions"]
# Any = [region, region, str, Rule]
entrances: list[Any] = logic["entrances"]
# Any = [group, [[id, str, region, Rule], ..]]
locations: list[Any] = logic["locations"]
location_to_id: dict[str, int] = logic["location_to_id"]
location_to_sort: dict[str, int] = logic["location_to_sort"]
location_groups: dict[str, list[str]] = logic["location_groups"]
# Any = [group, [[str, int], ..]]
items: list[Any] = logic["items"]
item_to_id: dict[str, int] = logic["item_to_id"]
item_to_class: dict[str, int] = logic["item_to_class"]
item_groups: dict[str, list[str]] = logic["item_groups"]
# TODO: put this in python
item_to_id["Glitches"] = None
item_to_class["Glitches"] = 7
fillers: dict[str, list[str]] = logic["fillers"]
level_items: list[str | None] = logic["level_items"]
piece: str = logic["piece"]
aCatapult: int = logic["aCatapult"]
aWin: int = logic["aWin"]

# gc can free a single dict
logic = None