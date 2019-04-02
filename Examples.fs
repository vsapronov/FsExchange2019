namespace FSharp.Exchange.Tests

module JsonData =
    open NUnit.Framework

    open FSharp.Data
    open FSharp.Data.JsonExtensions

    type Person = { Name: string; Born: int }

    [<Test>]
    let ``Record deserialization - optional member`` () =
        let data = JsonValue.Parse(""" { "name": "Tomas", "born": 1956 } """)
        let person = {
            Person.Name = data?name.AsString()
            Born = data?born.AsInteger()
        }
        Assert.AreEqual(person, { Person.Name = "Tomas"; Born = 1956 })

module JsonDataOptional =
    open NUnit.Framework

    open FSharp.Data
    open FSharp.Data.JsonExtensions

    type Person = { Name: string; Born: int; MiddleName: string option }

    [<Test>]
    let ``Record deserialization - optional member`` () =
        let data = JsonValue.Parse(""" { "name": "Teresa", "born": 1956 } """)
        let person = {
            Person.Name = data?name.AsString()
            Born = data?born.AsInteger()
            MiddleName =
                match data.TryGetProperty("middle_name") with
                | None -> None
                | Some JsonValue.Null -> None
                | Some value -> Some (value.AsString())
        }
        Assert.AreEqual({ Person.Name = "Teresa"; Born = 1956; MiddleName = None }, person)

module JsonProvider =
    open NUnit.Framework

    open FSharp.Data

    type Person = JsonProvider<""" { "name":"John", "age":94 } """>
        
    [<Test>]
    let ``Record deserialization`` () =
        let person = Person.Parse(""" { "name":"Tomas", "age":4 } """)
        Assert.AreEqual("Tomas", person.Name)
        Assert.AreEqual(4, person.Age)

module FSharpLuJsonNullSafety =
    open NUnit.Framework
    open Microsoft.FSharpLu.Json

    type Person = { Name: string; Born: int; MiddleName: string option }

    [<Test>]
    let ``Record deserialization - nulls`` () =
        let json1 = """ { "Name": "Teresa", "Born": 1956 } """
        let person1 = Compact.deserialize<Person> json1
        Assert.AreEqual({ Person.Name = "Teresa"; Born = 1956; MiddleName = None }, person1)
        let json2 = """ { "Name": null, "Born": 1956 } """
        let person2 = Compact.deserialize<Person> json2
        Assert.AreEqual({ Person.Name = null; Born = 1956; MiddleName = None }, person2)

module FSharpLuJsonSingleCaseUnion =
    open NUnit.Framework
    open Microsoft.FSharpLu.Json

    type Address = Address of string
    type Person = { Name: string; Address: Address }

    [<Test>]
    let ``Record deserialization - single case union`` () =
        let person = { Name = "Teresa"; Address = Address "10 Downing St" }
        let json = Compact.serialize person
        let expected = """{
  "Name": "Teresa",
  "Address": {
    "Address": "10 Downing St"
  }
}"""
        Assert.AreEqual(expected, json)
        
module ThothJson =
    open NUnit.Framework
    open Thoth.Json.Net

    type Person = {
        Name: string
        Born: int
        MiddleName: string option
    }
    
    [<Test>]
    let ``Record deserialization - optional member`` () =
        let json = """ { "name": "Teresa", "born": 1956 } """
        let person = Decode.Auto.unsafeFromString<Person>(json, isCamelCase=true)
        Assert.AreEqual({ Person.Name = "Teresa"; Born = 1956; MiddleName = None }, person)
                
module ThothJsonSingleCaseUnion =
    open NUnit.Framework
    open Thoth.Json.Net
    open Microsoft.FSharp.Core

    type Address = Value of string

    type Person = {
        Name: string
        Born: int
        MiddleName: string option
        Address: Address
    }

    [<Test>]
    let ``Record deserialization - non optional member`` () =
        let person = { Person.Name = "Teresa"; Born = 1956; MiddleName = None; Address = Address.Value "10 Downing St" }
        let json = Encode.Auto.toString(0, person, isCamelCase=true)
        let expected = """{"name":"Teresa","born":1956,"address":["Value","10 Downing St"]}"""
        Assert.AreEqual(expected, json)

module FSharpJson =
    open NUnit.Framework
    open FSharp.Json

    type Person = { Name: string; Born: int }

    [<Test>]
    let ``Deserialization + Serialization`` () =
        let expectedJson = """{"Name":"Teresa","Born":1956}"""
        let config = JsonConfig.create(unformatted = true)
        let person = Json.deserializeEx<Person> config expectedJson
        Assert.AreEqual({ Person.Name = "Teresa"; Born = 1956 }, person)
        let json = Json.serializeEx config person
        Assert.AreEqual(expectedJson, json)
        
module FSharpJsonNullSafety =
    open NUnit.Framework
    open FSharp.Json

    type Person = { Name: string; MiddleName: string option }

    [<Test>]
    let ``Null safety examples`` () =
        let json1 = """{"Name":"Teresa","MiddleName":null}"""
        let person1 = Json.deserialize<Person> json1
        Assert.AreEqual({ Person.Name = "Teresa"; MiddleName = None }, person1)
        let json2 = """{"Name":"Teresa","MiddleName":"Mary"}"""
        let person2 = Json.deserialize<Person> json2
        Assert.AreEqual({ Person.Name = "Teresa"; MiddleName = Some "Mary" }, person2)
        let json3 = """{"Name":null,"MiddleName":null}"""
        Assert.Throws<JsonDeserializationError>(fun () -> Json.deserialize<Person> json3 |> ignore) |> ignore
        
//module Chiron =
//    open NUnit.Framework
//    open Chiron
//    open Chiron.Operators
//
//    type Person = { Name: string; Born: int; MiddleName: string option }
//
//    module D = Json.Decode
//    module E = Json.Encode
//
//    module Person =
//        let make name born middleName = { Name = name; Born = born; MiddleName = middleName }
//        let encode x jObj =
//            jObj
//            |> E.required E.string "name" x.Name
//            |> E.required E.int "born" x.Born
//            |> E.required (E.optionWith E.string) "middle_name" x.MiddleName
//        let decode =
//            make
//            <*> D.required D.string "name"
//            <*> D.required D.int "born"
//            <!> D.required (D.optionWith D.string) "middle_name"