import { ValidationRules, validateTrigger} from "aurelia-validation";
import { autoinject, ObserverLocator } from 'aurelia-framework';
import { EventAggregator } from "aurelia-event-aggregator";
import { ValidationControllerFactory, ValidationController } from "aurelia-validation";
import * as moment from "moment";
import { HttpClient, json } from 'aurelia-fetch-client';

@autoinject()
export class PersonModel {

    constructor(
        http: HttpClient,
        validationControllerFactory: ValidationControllerFactory,
        eventAggregator: EventAggregator,
        observer: ObserverLocator
    ) {
        this.validation = validationControllerFactory.createForCurrentScope();
        this.validation.validateTrigger = validateTrigger.change;
        this.eventAggregator = eventAggregator;
        this.http = http;

        // click the save button when changing peeps
        this.eventAggregator.subscribe("Person_SelectionChanging", async (next) => {
            await this.saveClicked();
            next();
        });
    }

    determineActivationStrategy() {
        return "replace";
    }

    validation: ValidationController;
    eventAggregator: EventAggregator;
    http: HttpClient;
    canDelete: boolean = false;

    get addressChanged() {
        return JSON.stringify(this.orig) != JSON.stringify(this.data);
    }

    data: Person;
    orig: Person;

    activate(data: Person) {
        this.orig = data;
        this.data = Object.assign(new Person(), data);
        this.validation.reset();

        this.canDelete = (data.personID == null && !data.isPrimaryContact);
    }
    
    genderOptions = [
        { value: "", text: "[Select a Gender]"},
        { value: "F", text: "Female" },
        { value: "M", text: "Male" }
    ];

    //TODO: Load these from PCO or event DB via the API on app launch?
    gradeOptions = [
        { value: -2, text: "[Select a Grade]"},
        { value: "", text: "None / Not Started" },
        { value: -1, text: "Pre-K / Pre-School" },
        { value: 0, text: "Kindergarten" },
        { value: 1, text: "1st" },
        { value: 2, text: "2nd" },
        { value: 3, text: "3rd" },
        { value: 4, text: "4th" },
        { value: 5, text: "5th" },
        { value: 6, text: "6th" }
    ];

    get grade(): string {
        if (typeof this.data.grade === "number") {
            return this.data.grade.toString()
        } else {
            return "";
        }
    }
    set grade(value: string) {
        if (value) {
            this.data.grade = parseInt(value, 10);
        } else {
            this.data.grade = null;
        }       
    }

    async saveClicked() {
        let validationResult = await this.validation.validate({ object: this });

        if (!validationResult.valid) {
            return;
        }

        validationResult = await this.validation.validate({ object: this.data });

        if (!validationResult.valid) {
            return;
        }

        Object.assign(this.orig, this.data);

        this.eventAggregator.publish("Person_Updated", this.orig);
    }

    deleteClicked() {
        this.eventAggregator.publish("Person_Deleted", this.orig);
    }

    cancelClicked() {
        this.eventAggregator.publish("Person_Cancel", this.orig);
    }

    // Used to auto-fill the city/state when the zip code changes
    async zipChanged() {
        let response = await this.http.fetch(`api/Zip/${this.data.zip}`);

        if (response.ok) {
            let loc = await response.json();

            this.data.city = loc.city;
            this.data.state = loc.state;
        }
    }
}

export class Person {
    personID: string;
    firstName: string;
    lastName: string;
    child: boolean;
    emailAddress: string;
    phoneNumber: string;

    grade: number;
    gender: string;
    birthDate: string;

    medicalNotes: string;
    isPrimaryContact: boolean;
    street: string;
    city: string;
    state: string;
    zip: string;

    get displayName() {
        if (!this.firstName && !this.lastName) {
            return "New Person*";
        } else {
            return `${this.firstName} ${this.lastName}`;
        }
    }

    get age() {
        if (this.birthDate) {
            return moment().diff(moment(this.birthDate,"M/D/YYYY"), "years");
        } else {
            return "";
        }
    }

}

ValidationRules.ensure<PersonModel, string>("grade")
    .satisfies((value) => value === "" || value >= -1).when(x => x.data.child)
    .on(PersonModel);

ValidationRules.customRule(
    'date',
    (value, obj) => value === null || value === undefined || value == "" || (/\d{1,2}\/\d{1,2}\/\d{4}/i.test(value) && moment(value, "MM/DD/YYYY").isValid()),
    `\${$displayName} must be a valid date in the format M/D/YYYY.`
);

ValidationRules
    .ensure<Person, string>('firstName').required().maxLength(100).minLength(2)
    .ensure('lastName').required().maxLength(100).minLength(2)

    .ensure('emailAddress').required().when(x => !x.child && x.isPrimaryContact).email().maxLength(250)
    .ensure('street').required().when(x => !x.child && x.isPrimaryContact).maxLength(200)
    .ensure('city').required().when(x => !x.child && x.isPrimaryContact).minLength(3).maxLength(100)
    .ensure('state').required().when(x => !x.child && x.isPrimaryContact).maxLength(2).minLength(2)
    .ensure('zip').required().when(x => !x.child && x.isPrimaryContact).maxLength(5).minLength(5)

    .ensure('phoneNumber').required().when(x => !x.child).maxLength(13).minLength(10)

    .ensure('grade').satisfies((value) => value === "" || value >= -1).when(x => x.child)
    .ensure('gender').required().when(x => x.child)
    .ensure('birthDate').required().when(x => x.child).satisfiesRule("date")

    .on(Person);