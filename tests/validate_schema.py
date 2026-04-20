#!/usr/bin/env python3
"""
Schema validator - mirrors the C# models in Yafc.Model.Mobile.
Validates that every field in your real project.yafc maps to a model property.
Run: python3 validate_schema.py /mnt/user-data/uploads/project.yafc
"""
import json, sys

# Schemas - keys = expected properties (from C# models)
SETTINGS = {"milestones","itemFlags","miningProductivity","researchSpeedBonus",
            "researchProductivity","productivityTechnologyLevels","reactorSizeX",
            "reactorSizeY","PollutionCostModifier","spoilingRate"}
PREFS = {"time","itemUnit","fluidUnit","defaultBelt","defaultInserter",
         "inserterCapacity","sourceResources","favorites","targetTechnology",
         "iconScale","maxMilestonesPerTooltipLine","showMilestoneOnInaccessible"}
PROJECT = {"settings","preferences","sharedModuleTemplates","yafcVersion",
           "pages","displayPages"}
PAGE = {"contentType","guid","icon","name","content","scroll"}
TABLE = {"expanded","links","recipes","modules"}
RECIPE = {"recipe","icon","description","entity","fuel","fixedBuildings",
          "fixedFuel","fixedIngredient","fixedProduct","builtBuildings",
          "showTotalIO","enabled","tag","modules","subgroup","variants"}
LINK = {"goods","amount","algorithm"}
MODULES = {"beacon","list","beaconList"}
PAGE_MODULE_DEF = {"fillMiners","autoFillPayback","fillerModule","beacon",
                   "beaconModule","beaconsPerBuilding","overrideCrafterBeacons"}
MODULE_ENTRY = {"module","fixedCount"}
TYPED_REF = {"target","quality"}

def check(name, actual, expected):
    missing = expected - actual
    extra = actual - expected
    if missing: print(f"  [{name}] MISSING: {missing}")
    if extra: print(f"  [{name}] EXTRA: {extra}")
    return not (missing or extra)

def walk_table(t, path=""):
    ok = check(f"table{path}", set(t.keys()), TABLE)
    for i, link in enumerate(t.get("links",[])):
        ok &= check(f"link{path}[{i}]", set(link.keys()), LINK)
        if isinstance(link.get("goods"), dict):
            ok &= check(f"link{path}[{i}].goods", set(link["goods"].keys()), TYPED_REF)
    for i, r in enumerate(t.get("recipes",[])):
        ok &= check(f"recipe{path}[{i}]", set(r.keys()), RECIPE)
        if r.get("modules"):
            ok &= check(f"recipe{path}[{i}].modules", set(r["modules"].keys()), MODULES)
            for j, m in enumerate(r["modules"].get("list",[])):
                ok &= check(f"recipe{path}[{i}].modules.list[{j}]",
                            set(m.keys()), MODULE_ENTRY)
        if r.get("subgroup"):
            ok &= walk_table(r["subgroup"], f"{path}>sub[{i}]")
    if t.get("modules"):
        ok &= check(f"table{path}.modules", set(t["modules"].keys()), PAGE_MODULE_DEF)
    return ok

def main(path):
    with open(path) as f: d = json.load(f)
    print(f"File: {path}")
    print(f"Version: {d.get('yafcVersion')}\n")

    ok = check("project", set(d.keys()), PROJECT)
    ok &= check("settings", set(d["settings"].keys()), SETTINGS)
    ok &= check("preferences", set(d["preferences"].keys()), PREFS)

    prod_count = 0
    for i, p in enumerate(d["pages"]):
        ok &= check(f"page[{i}]({p.get('name')})", set(p.keys()), PAGE)
        if p["contentType"] == "Yafc.Model.ProductionTable":
            ok &= walk_table(p["content"], f":{p.get('name')}")
            prod_count += 1
    print(f"\nProductionTable pages walked: {prod_count}")
    print(f"Summary pages: {sum(1 for p in d['pages'] if p['contentType']=='Yafc.Model.Summary')}")
    print(f"\nResult: {'PASS - schema matches' if ok else 'FAIL - schema mismatches found'}")
    return 0 if ok else 1

if __name__ == "__main__":
    sys.exit(main(sys.argv[1] if len(sys.argv)>1 else "/mnt/user-data/uploads/project.yafc"))
