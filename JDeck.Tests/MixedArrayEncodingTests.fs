namespace JDeck.Tests

open System.Text.Json.Nodes
open Microsoft.VisualStudio.TestTools.UnitTesting
open JDeck

type Vector3 = { X: float32; Y: float32; Z: float32 }

type Quaternion = {
  X: float32
  Y: float32
  Z: float32
  W: float32
}

[<RequireQualifiedAccess>]
type MapObjectShape =
  | Box of size: Vector3
  | Sphere of radius: float32

type SpawnProperties = {
  IsPlayerSpawn: bool
  EntityGroup: string option
  MaxSpawns: int
  Faction: string option
}

type TeleportProperties = {
  TargetMap: string option
  TargetObjectName: string
}

type MapObjectData =
  | Spawn of spawn: SpawnProperties
  | Teleport of teleport: TeleportProperties
  | Trigger of triggerId: string

type MapObject = {
  Id: int
  Name: string
  Position: Vector3
  Rotation: Quaternion option
  Shape: MapObjectShape
  Data: MapObjectData
}

[<AutoOpen>]
module Helpers =
  let encodeVector3 (v: Vector3) : JsonNode =
    Json.object [
      "x", Encode.single v.X
      "y", Encode.single v.Y
      "z", Encode.single v.Z
    ]

  let encodeQuaternion (q: Quaternion) : JsonNode =
    Json.object [
      "x", Encode.single q.X
      "y", Encode.single q.Y
      "z", Encode.single q.Z
      "w", Encode.single q.W
    ]

  let encodeMapObjectShape: Encoder<MapObjectShape> =
    function
    | MapObjectShape.Box size ->
      Json.sequence [ Encode.string "Box"; encodeVector3 size ]
    | MapObjectShape.Sphere radius ->
      Json.sequence [ Encode.string "Sphere"; Encode.single radius ]

  let encodeSpawnProperties (props: SpawnProperties) : JsonNode =
    Json.object [
      "isPlayerSpawn", Encode.boolean props.IsPlayerSpawn
      match props.EntityGroup with
      | Some group -> "entityGroup", Encode.string group
      | None -> ()
      "maxSpawns", Encode.int props.MaxSpawns
      match props.Faction with
      | Some faction -> "faction", Encode.string faction
      | None -> ()
    ]

  let encodeTeleportProperties (props: TeleportProperties) : JsonNode =
    Json.object [
      match props.TargetMap with
      | Some map -> "targetMap", Encode.string map
      | None -> ()
      "targetObjectName", Encode.string props.TargetObjectName
    ]

  let encodeMapObjectData: Encoder<MapObjectData> =
    function
    | MapObjectData.Spawn props ->
      Json.sequence [ Encode.string "Spawn"; encodeSpawnProperties props ]
    | MapObjectData.Teleport props ->
      Json.sequence [ Encode.string "Teleport"; encodeTeleportProperties props ]
    | MapObjectData.Trigger id ->
      Json.sequence [ Encode.string "Trigger"; Encode.string id ]

  let encodeMapObject (obj: MapObject) : JsonNode =
    Json.object [
      "id", Encode.int obj.Id
      "name", Encode.string obj.Name
      "position", encodeVector3 obj.Position
      match obj.Rotation with
      | Some rot -> "rotation", encodeQuaternion rot
      | None -> ()
      "shape", encodeMapObjectShape obj.Shape
      "data", encodeMapObjectData obj.Data
    ]

[<TestClass>]
type MixedArrayEncodingTests() =

  [<TestMethod>]
  member _.``Encode.mixedSeq can add JsonNodes to an existing JsonArray``() =
    let arr = JsonArray()

    let result =
      Encode.mixedSeq
        [ Encode.int 1; Encode.string "hello"; Encode.boolean true ]
        arr

    let expected = "[1,\"hello\",true]"
    Assert.AreEqual<string>(expected, result.ToJsonString())

  [<TestMethod>]
  member _.``Json.sequence can create a mixed type array``() =
    let result =
      Json.sequence [
        Encode.int 1
        Encode.string "string"
        Encode.boolean true
      ]

    let expected = "[1,\"string\",true]"
    Assert.AreEqual<string>(expected, result.ToJsonString())

  [<TestMethod>]
  member _.``Json.sequence can encode empty arrays``() =
    let result = Json.sequence []
    let expected = "[]"
    Assert.AreEqual<string>(expected, result.ToJsonString())

  [<TestMethod>]
  member _.``Json.sequence can nest Json.sequence calls``() =
    let result =
      Json.sequence [
        Encode.int 1
        Encode.string "string"
        Json.sequence [ Encode.int 2; Encode.string "nested" ]
        Json.sequence []
      ]

    let expected = "[1,\"string\",[2,\"nested\"],[]]"
    Assert.AreEqual<string>(expected, result.ToJsonString())

  [<TestMethod>]
  member _.``Box shape encodes as mixed array with tag and size``() =
    let size = { X = 10.0f; Y = 20.0f; Z = 30.0f }
    let shape = MapObjectShape.Box size
    let encoded = encodeMapObjectShape shape

    let expected = """["Box",{"x":10,"y":20,"z":30}]"""
    Assert.AreEqual<string>(expected, encoded.ToJsonString())

  [<TestMethod>]
  member _.``Sphere shape encodes as mixed array with tag and radius``() =
    let shape = MapObjectShape.Sphere 15.5f
    let encoded = encodeMapObjectShape shape

    let expected = """["Sphere",15.5]"""
    Assert.AreEqual<string>(expected, encoded.ToJsonString())

  [<TestMethod>]
  member _.``Spawn data encodes as mixed array with tag and properties``() =
    let props = {
      IsPlayerSpawn = true
      EntityGroup = Some "Enemies"
      MaxSpawns = 10
      Faction = Some "Alliance"
    }

    let data = MapObjectData.Spawn props
    let encoded = encodeMapObjectData data

    let expected =
      """["Spawn",{"isPlayerSpawn":true,"entityGroup":"Enemies","maxSpawns":10,"faction":"Alliance"}]"""

    Assert.AreEqual<string>(expected, encoded.ToJsonString())

  [<TestMethod>]
  member _.``Teleport data encodes as mixed array with tag and properties``() =
    let props = {
      TargetMap = Some "MapB"
      TargetObjectName = "TeleportDestination"
    }

    let data = MapObjectData.Teleport props
    let encoded = encodeMapObjectData data

    let expected =
      """["Teleport",{"targetMap":"MapB","targetObjectName":"TeleportDestination"}]"""

    Assert.AreEqual<string>(expected, encoded.ToJsonString())

  [<TestMethod>]
  member _.``Trigger data encodes as mixed array with tag and id``() =
    let data = MapObjectData.Trigger "trigger-123"
    let encoded = encodeMapObjectData data

    let expected = """["Trigger","trigger-123"]"""
    Assert.AreEqual<string>(expected, encoded.ToJsonString())

  [<TestMethod>]
  member _.``Complete MapObject with Spawn data encodes correctly``() =
    let obj = {
      Id = 1
      Name = "Player Spawn Point"
      Position = { X = 100.0f; Y = 0.0f; Z = 200.0f }
      Rotation = None
      Shape = MapObjectShape.Box { X = 5.0f; Y = 2.0f; Z = 5.0f }
      Data =
        MapObjectData.Spawn {
          IsPlayerSpawn = true
          EntityGroup = Some "Players"
          MaxSpawns = 1
          Faction = None
        }
    }

    let encoded = encodeMapObject obj

    let expected =
      """{"id":1,"name":"Player Spawn Point","position":{"x":100,"y":0,"z":200},"shape":["Box",{"x":5,"y":2,"z":5}],"data":["Spawn",{"isPlayerSpawn":true,"entityGroup":"Players","maxSpawns":1}]}"""

    Assert.AreEqual<string>(expected, encoded.ToJsonString())

  [<TestMethod>]
  member _.``Complete MapObject with Teleport data and Rotation encodes correctly``
    ()
    =
    let obj = {
      Id = 2
      Name = "Teleporter to Zone B"
      Position = { X = 50.0f; Y = 10.0f; Z = 50.0f }
      Rotation =
        Some {
          X = 0.0f
          Y = 1.0f
          Z = 0.0f
          W = 1.0f
        }
      Shape = MapObjectShape.Sphere 2.5f
      Data =
        MapObjectData.Teleport {
          TargetMap = Some "ZoneB"
          TargetObjectName = "TeleportExit"
        }
    }

    let encoded = encodeMapObject obj

    let expected =
      """{"id":2,"name":"Teleporter to Zone B","position":{"x":50,"y":10,"z":50},"rotation":{"x":0,"y":1,"z":0,"w":1},"shape":["Sphere",2.5],"data":["Teleport",{"targetMap":"ZoneB","targetObjectName":"TeleportExit"}]}"""

    Assert.AreEqual<string>(expected, encoded.ToJsonString())

  [<TestMethod>]
  member _.``Complete MapObject with Trigger data encodes correctly``() =
    let obj = {
      Id = 3
      Name = "Door Trigger"
      Position = { X = 75.0f; Y = 5.0f; Z = 75.0f }
      Rotation = None
      Shape = MapObjectShape.Box { X = 3.0f; Y = 3.0f; Z = 0.5f }
      Data = MapObjectData.Trigger "door-trigger-001"
    }

    let encoded = encodeMapObject obj

    let expected =
      """{"id":3,"name":"Door Trigger","position":{"x":75,"y":5,"z":75},"shape":["Box",{"x":3,"y":3,"z":0.5}],"data":["Trigger","door-trigger-001"]}"""

    Assert.AreEqual<string>(expected, encoded.ToJsonString())
