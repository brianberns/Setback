import { Record } from "../Client/src/.fable/fable-library.3.2.9/Types.js";
import { list_type, unit_type, lambda_type, class_type, option_type, record_type, int32_type, string_type } from "../Client/src/.fable/fable-library.3.2.9/Reflection.js";

export class Student extends Record {
    constructor(Name, Age) {
        super();
        this.Name = Name;
        this.Age = (Age | 0);
    }
}

export function Student$reflection() {
    return record_type("Shared.Student", [], Student, () => [["Name", string_type], ["Age", int32_type]]);
}

export class IStudentApi extends Record {
    constructor(StudentByName, AllStudents) {
        super();
        this.StudentByName = StudentByName;
        this.AllStudents = AllStudents;
    }
}

export function IStudentApi$reflection() {
    return record_type("Shared.IStudentApi", [], IStudentApi, () => [["StudentByName", lambda_type(string_type, class_type("Microsoft.FSharp.Control.FSharpAsync`1", [option_type(Student$reflection())]))], ["AllStudents", lambda_type(unit_type, class_type("Microsoft.FSharp.Control.FSharpAsync`1", [list_type(Student$reflection())]))]]);
}

