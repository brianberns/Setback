module App

open Browser.Dom
open Fable.Remoting.Client
open Shared

// studentApi : IStudentApi
let studentApi =
    Remoting.createApi()
        |> Remoting.buildProxy<IStudentApi>

// Get a reference to our button and cast the Element to an HTMLButtonElement
let myButton = document.querySelector(".my-button") :?> Browser.Types.HTMLButtonElement
let myList = document.querySelector(".my-list") :?> Browser.Types.HTMLUListElement

// Register our listener
myButton.onclick <- fun _ ->
    async {
        let! students = studentApi.AllStudents()
        for student in students do
            let item =
                document.createElement("li")
                    |> myList.appendChild
            let text = sprintf "Student %s is %d years old\n" student.Name student.Age
            document.createTextNode(text)
                |> item.appendChild
                |> ignore
    } |> Async.StartImmediate
