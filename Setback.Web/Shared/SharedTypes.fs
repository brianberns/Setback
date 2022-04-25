namespace Shared

type Student =
    {
        Name : string
        Age : int
    }

type IStudentApi =
    {
        StudentByName : string -> Async<Option<Student>>
        AllStudents : unit -> Async<List<Student>>
    }
