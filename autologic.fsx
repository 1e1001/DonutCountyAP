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

let data_value (data : XmlElement) =
    match data.GetAttribute "ss:Type" with
    | "String" -> DataString data.InnerText
    | "Number" -> DataNumber data.InnerText
    | ty -> BadDataType ty |> raise

let get_table (doc: XmlDocument) ns name (columnNames: string array) construct =
    match doc.SelectNodes($"//ss:Worksheet[@ss:Name='{name}']/ss:Table/ss:Row", ns) |> xnl_seq |> List.ofSeq with
    | header :: rows ->
        let headerNames =
            header.SelectNodes("ss:Cell/ss:Data", ns)
            |> xnl_seq
            |> Seq.map (fun node ->
                (downcast node |> data_value).String
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
                result[index] <- downcast cell.SelectSingleNode("ss:Data", ns) |> data_value
                index <- index + 1
            index <- -1
            construct (fun () ->
                index <- index + 1
                result[index]
            )
        )
    | _ -> BadTable "no header" |> raise

let verify_no_duplicate_ids (list: int list) =
    let diff = list.Length - (List.distinct list).Length
    if diff > 0 then
        BadContent $"{diff} duplicate ids" |> raise
        

(* TODO: verification features
- duplicate ids
- invalid type

ok now i decide what my output streams are
    only output two files

locations
-> python regions
    region for every level item
    region for every level location
    def autologic_regions(callback):
        callback(start, end)
        ...
-> python locations
    everything but event
    preprocess event into defined rulesbuilder shorthands
    group by type and preprocess type into options
    def autologic_locations(options, callback):
        if condition:
            callback(id, sort, name, region, rules)
            ...
        ...
-> c# events
    event, type, and id
    static Dictionary<string, Location> Events = {
        {event, new(type, id)}
    };
-> c# tracker info
    change once i make the tracker
    static DebugTracker[] Tracker = [
        new(id, name)
    ];

items
-> python items
-> c# items
    id and code
    public enum ItemId {
        code = id,
        ...
    }


*)

type ItemRow = { id: int; code: string; name: string; quantity: int; type_: string; value: string; class_: string }
type LocationRow = { id: int; sort: string; name: string; type_: string; region: string; rules: string; event: string }

let location_rules (itemMap: Map<string, string>) (rule: string) =
    if rule = "" then
        "True_()"
    else
        // no quotes in string :(
        let itemSalt = itemMap["Salt"]
        let itemPepper = itemMap["Pepper"]
        rule.Split(',')
        |> Seq.ofArray
        |> Seq.map (fun rule ->
            match rule with
            | "SaltPepperOne" -> $"HasFlag(\"{itemSalt}\") & HasFlag(\"{itemPepper}\")"
            | "SaltPepperAll" -> $"HasFlag(\"{itemSalt}\", 2) & HasFlag(\"{itemPepper}\", 3)"
            | rule when itemMap.ContainsKey(rule) -> $"HasFlag(\"{itemMap[rule]}\")"
            | rule -> BadContent $"bad rule {rule}" |> raise
        )
        |> String.concat " & "
let location_condition type_ =
    match type_ with
    | "Delivery" -> "True"
    | "Segment" -> "o.level_segments"
    | "Achievement" -> "o.achievements"
    | "Victory" -> "False"
    | "SnakeDanger" -> "o.snake_danger"
    | "Catapult" -> "o.buy_catapult"
    | "SaltAndPepper" -> "o.salt_and_pepper"
    | "HackProtocol" -> "o.hack_protocol"
    | type_ -> BadContent $"bad location type {type_}" |> raise

let item_condition type_ value =
    match type_ with
    | "Level" -> "OptionsFilter(options.Levels, options.Levels.option_true)"
    | "Flag" when value = "" -> "None"
    | "Flag" ->
        let key = value.Split(',')[0]
        $"OptionsFilter(options.{key}, options.{value})"
    | type_ -> BadContent $"bad item type {type_}" |> raise

let item_class (class_: string) =
    class_.Split(',')
    |> Seq.ofArray
    |> Seq.map (fun class_ -> $"ItemClassification.{class_}")
    |> String.concat " | "

let main =
    use docStream = new FileStream("./logic.xml", FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
    let doc = new XmlDocument()
    doc.Load docStream
    let ns = new XmlNamespaceManager(doc.NameTable)
    ns.AddNamespace("ss", "urn:schemas-microsoft-com:office:spreadsheet")
    let items =
        get_table doc ns "items" 
            [| "id"; "code"; "name"; "quantity"; "type"; "value"; "class" |]
            (fun f -> { id = f().Int; code = f().String; name = f().String; quantity = f().Int; type_ = f().String; value = f().String; class_ = f().String })
    items |> List.map (fun row -> row.id) |> verify_no_duplicate_ids
    let locations =
        get_table doc ns "locations"
            [| "id"; "sort"; "name"; "type"; "region"; "rules"; "event" |]
            (fun f -> { id = f().Int; sort = f().String; name = f().String; type_ = f().String; region = f().String; rules = f().String; event = f().String })
    locations |> List.map (fun row -> row.id) |> verify_no_duplicate_ids

    use pyStream = new FileStream("./donutcounty/autologic.py", FileMode.Create, FileAccess.Write, FileShare.ReadWrite)
    use pyStream = new StreamWriter(pyStream)
    pyStream.WriteLine "# generated by autologic.fsx
from .autologic_extra import True_, HasFlag, OptionsFilter, ItemClassification, options

def regions(f):"
    let itemMap =
        items
        |> List.map (fun item -> (item.code, item.name))
        |> Map.ofList
    for item in items |> List.filter (fun item -> item.type_ = "Level") do
        sprintf "    f(\"%s0\", \"Menu\", HasFlag(\"%s\"))" item.value item.name
        |> pyStream.WriteLine
    // TODO: if i wanna give regions reasonable names, generate it from the completion location name (or ": Finish delivery")
    // and make the logic region internal to the generator
    for location in locations |> List.filter(fun location -> location.type_ = "Delivery" || location.type_ = "Segment") do
        let prefix = location.region.Substring(0, location.region.Length - 1)
        let suffix = location.region.Substring(location.region.Length - 1) |> int
        let previous_region = prefix + string suffix
        sprintf "    f(\"%s\", \"%s\", %s)" location.region previous_region (location_rules itemMap location.rules)
        |> pyStream.WriteLine
    pyStream.WriteLine "
def locations(f):"
    for type_, locations in locations |> List.groupBy (fun location -> location.type_) do
        sprintf "    c = lambda o: %s" (location_condition type_)
        |> pyStream.WriteLine
        for location in locations do
            sprintf "    f(%d, \"%s\", \"%s\", \"%s\", %s, c)" location.id location.sort location.name location.region (location_rules itemMap location.rules)
            |> pyStream.WriteLine
    pyStream.WriteLine "
def items(f):"
    for item in items do
        sprintf "    f(%d, \"%s\", %d, %s, %s)" item.id item.name item.quantity (item_condition item.type_ item.value) (item_class item.class_)
        |> pyStream.WriteLine

    use csStream = new FileStream("./Randomizer/AutoLogic.cs", FileMode.Create, FileAccess.Write, FileShare.ReadWrite)
    use csStream = new StreamWriter(csStream)
    csStream.WriteLine "// generated by autologic.fsx
using System.Collections.Generic;

namespace DonutCountyAP.Randomizer;

public partial class AutoLogic
{
    static Dictionary<string, Location> Events = new() {"
    for location in locations do
        if location.event <> "" then
            sprintf "        [\"%s\"] = new(%d, LocationType.%s)," location.event location.id location.type_
            |> csStream.WriteLine
    csStream.WriteLine "    };
    static DebugTracker[] Tracker = ["
    for location in locations do
        if location.event <> "" then
            sprintf "        new(%d, \"%s\")," location.id location.name
            |> csStream.WriteLine
    csStream.WriteLine "    ];
}"
main