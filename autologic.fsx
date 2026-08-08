open System.Xml;
open System.IO

let xnl_seq (list : XmlNodeList) =
    seq { for node in list -> node }

exception BadDataType of string
exception BadTable of string
exception BadContent of string

type DataValue =
    | DataString of string
    | DataNumber of string
    | DataNone
    member this.String =
        match this with
        | DataString text -> text
        | DataNumber text -> text
        | DataNone -> ""
    member this.Int =
        match this with
        | DataNumber text -> int text
        | value -> sprintf "expected int got %A" this |> BadDataType |> raise

let dataValue (data : XmlElement) =
    match data.GetAttribute "ss:Type" with
    | "String" -> DataString data.InnerText
    | "Number" -> DataNumber data.InnerText
    | ty -> BadDataType ty |> raise

let getTable (doc: XmlDocument) ns name (columnNames: string array) construct =
    match doc.SelectNodes($"//ss:Worksheet[@ss:Name='{name}']/ss:Table/ss:Row", ns) |> xnl_seq |> List.ofSeq with
    | header :: rows ->
        let headerNames =
            header.SelectNodes("ss:Cell/ss:Data", ns)
            |> xnl_seq
            |> Seq.map (fun node ->
                (downcast node |> dataValue).String
            )
            |> Array.ofSeq
        if headerNames <> columnNames then
            sprintf "header mismatch %A <> %A" headerNames columnNames |> BadTable |> raise
        let columns = columnNames.Length
        rows
        |> List.map (fun row ->
            let mutable index = 0
            let mutable result = Array.create columns DataNone
            for cell in row.SelectNodes("ss:Cell", ns) do
                let cell: XmlElement = downcast cell
                let newIndex = cell.GetAttribute "ss:Index"
                if newIndex <> "" then
                    index <- int newIndex - 1
                result[index] <- downcast cell.SelectSingleNode("ss:Data", ns) |> dataValue
                index <- index + 1
            index <- -1
            construct (fun () ->
                index <- index + 1
                result[index]
            )
        )
    | _ -> BadTable "no header" |> raise

let verifyNoDuplicates (list: 'a list) =
    let diff = list.Length - (List.distinct list).Length
    if diff > 0 then
        BadContent $"{diff} duplicate ids" |> raise

type ItemRow = { id: int; code: string; name: string; quantity: int; type_: string; value: string; class_: string }
type LocationRow = { id: int; tracker: string; name: string; type_: string; event: string; region: string; rules: string }

let locationRules (itemMap: Map<string, string>) (rule: string) =
    if rule = "" then
        "True_()"
    else
        // no quotes in string :(
        let itemSalt = itemMap["Salt"]
        let itemPepper = itemMap["Pepper"]
        let itemSnakeDanger = itemMap["SnakeDanger"]
        rule.Split ' '
        |> Seq.ofArray
        |> Seq.map (fun rule ->
            match rule with
            | "" | "&" | "|" | "(" | ")" -> rule
            | "SnakeDangerAll" -> $"HasFlag(\"{itemSnakeDanger}\", 4)"
            | "SaltPepperOne" -> $"HasFlag(\"{itemSalt}\") & HasFlag(\"{itemPepper}\")"
            | "SaltPepperAll" -> $"HasFlag(\"{itemSalt}\", 2) & HasFlag(\"{itemPepper}\", 3)"
            | rule when rule.StartsWith("Catapult") && itemMap.ContainsKey(rule) -> $"HasFlag(\"Catapult\") & HasFlag(\"{itemMap[rule]}\")"
            | rule when itemMap.ContainsKey(rule) -> $"HasFlag(\"{itemMap[rule]}\")"
            | rule -> BadContent $"bad rule {rule}" |> raise
        )
        |> String.concat " "
let locationCondition type_ =
    match type_ with
    | "Delivery" -> "True"
    | "Segment" -> "o.level_segments"
    | "Achievement" -> "o.achievements"
    //| "Victory" -> "False"
    | "SnakeDanger" -> "o.snake_danger"
    | "Catapult" -> "o.buy_catapult"
    | "SaltAndPepper" -> "o.salt_and_pepper"
    | "HackProtocol" -> "o.hack_protocol"
    | type_ -> BadContent $"bad location type {type_}" |> raise
let locationSortOrder (tracker: string) =
    (tracker.Split ',')[0]

let itemCondition type_ value =
    match type_ with
    | "Level" -> "[OptionFilter(options.Levels, options.Levels.option_true)]"
    | "Filler" -> "[]"
    | "Flag" when value = "" -> "[]"
    | "Flag" ->
        let key = (value.Split '.')[0]
        $"[OptionFilter(options.{key}, options.{value})]"
    | type_ -> BadContent $"bad item type {type_}" |> raise
let itemClass (class_: string) =
    class_.Split ','
    |> Seq.ofArray
    |> Seq.map (fun class_ -> $"ItemClassification.{class_}")
    |> String.concat " | "

let trackerParts (tracker: string) =
    if tracker.Substring(0, 1) <> "d" || tracker.Substring(3, 1) <> "c" then
        BadContent $"bad tracker {tracker}" |> raise
    int (tracker.Substring(1, 2)), int (tracker.Substring(4, 1)), tracker.Substring(5)

let main =
    use docStream = new FileStream("./logic.xml", FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
    let doc = new XmlDocument()
    doc.Load docStream
    let ns = new XmlNamespaceManager(doc.NameTable)
    ns.AddNamespace("ss", "urn:schemas-microsoft-com:office:spreadsheet")
    let items =
        getTable doc ns "items" 
            [| "id"; "code"; "name"; "quantity"; "type"; "value"; "class" |]
            (fun f -> { id = f().Int; code = f().String; name = f().String; quantity = f().Int; type_ = f().String; value = f().String; class_ = f().String })
    items |> List.map (fun row -> row.id) |> verifyNoDuplicates
    let locations =
        getTable doc ns "locations"
            [| "id"; "tracker"; "name"; "type"; "event"; "region"; "rules" |]
            (fun f -> { id = f().Int; tracker = f().String; name = f().String; type_ = f().String; event = f().String; region = f().String; rules = f().String })
    locations |> List.map (fun row -> row.id) |> verifyNoDuplicates
    locations |> List.map (fun row -> row.tracker) |> verifyNoDuplicates
    let itemMap =
        items
        |> List.map (fun item -> (item.code, item.name))
        |> Map.ofList

    use pyStream = new FileStream("./donutcounty/autologic.py", FileMode.Create, FileAccess.Write, FileShare.ReadWrite)
    use pyStream = new StreamWriter(pyStream)
    pyStream.WriteLine "# generated by autologic.fsx
from BaseClasses import ItemClassification
from rule_builder.options import OptionFilter
from rule_builder.rules import Has, True_
from . import options

LOCATION_NAME_TO_ID = {"
    for location in locations |> List.filter (fun location -> location.type_ <> "Victory") do
        sprintf "    \"%s\": %d," location.name location.id
        |> pyStream.WriteLine
    pyStream.WriteLine "}
LOCATION_SORT_ORDER = {"
    for location in locations |> List.filter (fun location -> location.type_ <> "Victory") do
        sprintf "    \"%s\": \"%s\"," location.name (locationSortOrder location.tracker)
        |> pyStream.WriteLine
    pyStream.WriteLine "}
LEVEL_ENTRANCES = ["
    for item in items |> List.filter (fun item -> item.type_ = "Level") do
        sprintf "    (\"%s0\", \"%s\")," item.value item.name
        |> pyStream.WriteLine
    pyStream.WriteLine "    (\"Aftermath0\", None),
]
ITEM_NAME_TO_ID = {"
    for item in items do
        sprintf "    \"%s\": %d," item.name item.id
        |> pyStream.WriteLine
    pyStream.WriteLine "}
ITEM_DATA = {"
    for item in items do
        sprintf "    \"%s\": (%s, %s)," item.name (itemClass item.class_) (itemCondition item.type_ item.value)
        |> pyStream.WriteLine
    pyStream.WriteLine "}
ITEM_FILLER = {"
    for item in items |> List.filter (fun item -> item.type_ = "Filler") do
        sprintf "    \"%s\": \"%s\"," item.value item.name
        |> pyStream.WriteLine
    pyStream.WriteLine "}
    
def HasFlag(name: str, amount: int = 1):
    return Has(name, amount, options=ITEM_DATA[name][1], filtered_resolution=True)

def regions(f):"
    for item in items |> List.filter (fun item -> item.type_ = "Level") do
        // currently, the initial entrance rules are given by rules.py
        //sprintf "    f(\"%s0\", \"Menu\", HasFlag(\"%s\"))" item.value item.name
        //|> pyStream.WriteLine
        sprintf "    f(\"%s0\", \"Menu\", True_())" item.value
        |> pyStream.WriteLine
    // TODO: if i wanna give regions reasonable names, generate it from the completion location name (or ": Finish")
    // and make the logic region internal to the generator
    for location in locations |> List.filter(fun location -> location.type_ = "Delivery" || location.type_ = "Segment") do
        let prefix = location.region.Substring(0, location.region.Length - 1)
        let suffix = location.region.Substring(location.region.Length - 1) |> int
        let previous_region = prefix + string (suffix - 1)
        sprintf "    f(\"%s\", \"%s\", %s)" location.region previous_region (locationRules itemMap location.rules)
        |> pyStream.WriteLine
    pyStream.WriteLine "
def locations(o, f):"
    for type_, locations in locations |> List.groupBy (fun location -> location.type_) do
        if type_ <> "Victory" then
            sprintf "    if %s:" (locationCondition type_)
            |> pyStream.WriteLine
            for location in locations do
                sprintf "        f(%d, \"%s\", \"%s\", %s)" location.id location.name location.region (locationRules itemMap location.rules)
                |> pyStream.WriteLine
    pyStream.WriteLine "
def items(o, f):"
    for condition, items in items |> List.groupBy (fun item -> itemCondition item.type_ item.value) do
        if condition = "[]" then
            for item in items do
                sprintf "    f(%d, \"%s\")" item.quantity item.name
                |> pyStream.WriteLine
        else
            sprintf "    if ITEM_DATA[\"%s\"][1][0].check(o):" items[0].name
            |> pyStream.WriteLine
            for item in items do
                sprintf "        f(%d, \"%s\")" item.quantity item.name
                |> pyStream.WriteLine

    use csStream = new FileStream("./Randomizer/AutoLogic.cs", FileMode.Create, FileAccess.Write, FileShare.ReadWrite)
    use csStream = new StreamWriter(csStream)
    csStream.WriteLine "// generated by autologic.fsx
using System.Collections.Generic;

namespace DonutCountyAP.Randomizer;

public enum ItemId {
    None,"
    for item in items |> List.sortBy (fun item -> item.id) do
        sprintf "    %s," item.code
        |> csStream.WriteLine
    csStream.WriteLine "    Length
}

public partial class AutoLogic
{
    public static readonly Dictionary<string, Location> EVENTS = new() {"
    for location in locations do
        if location.event <> "" then
            sprintf "        [\"%s\"] = new(%d, LocationType.%s)," location.event location.id location.type_
            |> csStream.WriteLine
    csStream.WriteLine "    };
    public static readonly DebugTracker[] DEBUG_TRACKER = ["
    for location in locations do
        sprintf "        new(\"%s\", new(%d, LocationType.%s))," location.name location.id  location.type_
        |> csStream.WriteLine
    csStream.WriteLine "    ];
    public static readonly LevelSelect[] LEVEL_SELECT = ["
    // TODO: actually do all the inner loop logic while printing instead of before
    let splitTrackers =
        locations
        |> List.collect (fun location ->
            location.tracker.Split ','
            |> List.ofArray
            |> List.filter (fun tracker -> tracker.Length > 0)
            |> List.map (fun tracker -> trackerParts tracker, (location.id, location.type_))
        )
        |> List.groupBy (fun ((location, _, _), _) -> location)
        |> List.sortBy fst
        |> List.map(fun (_, level) ->
            level
            |> List.groupBy (fun ((_, row, _), _) -> row)
            |> List.sortBy fst
            |> List.map(fun (_, row) ->
                row
                |> List.sortBy (fun ((_, _, sort), _) -> sort)
                |> List.map (fun (_, (id, type_)) -> $"new({id}, LocationType.{type_})")
                |> String.concat ","
            )
            |> String.concat ",new(-1, LocationType.Victory),"
        )
    let levels = seq {
        yield items |> List.filter (fun item -> item.type_ = "Level") |> List.map (fun item -> item.code)
        yield ["None"]
    }
    for i, item in levels |> List.concat |> List.indexed do
        sprintf "        new(ItemId.%s, [%s])," item splitTrackers[i]
        |> csStream.WriteLine
    csStream.WriteLine "    ];
}"
main