namespace Server

open Suave
open Suave.Logging
open Suave.Operators

open Fable.Remoting.Server
open Fable.Remoting.Suave

open Shared

module Program =

    let getStudents() =
      async {
        return [
            { Name = "Mike";  Age = 23; }
            { Name = "John";  Age = 22; }
            { Name = "Diana"; Age = 22; }
        ]
      }

    let findStudentByName name =
      async {
        let! students = getStudents()
        let student = List.tryFind (fun student -> student.Name = name) students
        return student
      }

    let studentApi : IStudentApi =
        {
            StudentByName = findStudentByName
            AllStudents = getStudents
        }

    let logger = Targets.create LogLevel.Info [||]
    let webApp =
        (Remoting.createApi()
            |> Remoting.fromValue studentApi
            |> Remoting.buildWebPart)
            >=> Filters.logWithLevelStructured LogLevel.Info logger Filters.logFormatStructured

    // start the web server
    let config =
        { defaultConfig with
            bindings = [ HttpBinding.createSimple HTTP "127.0.0.1" 5000 ] }
    startWebServer config webApp
