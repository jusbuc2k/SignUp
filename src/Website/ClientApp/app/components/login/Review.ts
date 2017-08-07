import { HttpClient, json } from 'aurelia-fetch-client';
import { autoinject } from 'aurelia-framework';
import { Person } from "./Person";
import { ValidationControllerFactory, ValidationController } from "aurelia-validation";
import { EventAggregator } from "aurelia-event-aggregator";
import { Router } from "aurelia-router";

//class PersonWithCost extends Person {

//    get cost() {
//        if (this.child && this.age < 4 && parseInt(this.grade,10) === -1) {
//            return 15;
//        } else if (this.child) {
//            return 30;
//        } else {
//            return null;
//        }
//    }
//}

interface IFeeSchedule {
    maxAge: number;
    maxGrade: number;
    child: boolean;
    cost: number;
    group: string
}

@autoinject()
export class FamilyModel {
    constructor(http: HttpClient, router: Router, validationControllerFactory: ValidationControllerFactory, eventAggregator: EventAggregator) {
        this.http = http;
        this.router = router;
        this.validation = validationControllerFactory.createForCurrentScope();
    }

    protected http: HttpClient;
    protected validation: ValidationController;
    protected router: Router;

    hid: string;
    people: Person[] = [];
    new: boolean;
    fees: IFeeSchedule[] = [];

    errors: string[] = [];
    totalCost: number;

    activate(params) {
        let house = JSON.parse(sessionStorage.getItem(`Household_${params.id}`));

        this.hid = house.id;
        this.new = house.new;
        this.people = house.people.map(p => Object.assign(new Person(), p));

        this.fees = [
            { child: true, maxGrade: -1, maxAge: 3, cost: 15, group: "Puggles" },
            { child: true, maxGrade: -1, maxAge: 5, cost: 30, group: "Cubbies" },
            { child: true, maxGrade: 2, maxAge: 99, cost: 30, group: "Sparks" },
            { child: true, maxGrade: 6, maxAge: 99, cost: 30, group: "T&T" },
        ];

        this.totalCost = 0;
        
        if (this.fees.length > 0) {
            this.people.forEach(p => {
                let fee = this.fees.find(f => f.child === p.child && f.maxAge >= p.age && f.maxGrade >= parseInt(p.grade, 10));

                if (fee) {
                    p["cost"] = fee.cost;
                    p["group"] = fee.group;
                    this.totalCost += fee.cost;
                }
            });
        }

        this.hid = house.id;
    }

    async nextClicked() {
        this.errors.splice(0, this.errors.length);

        if (this.people.filter(x => x.child == true).length <= 0) {
            this.errors.push("At least one child must be added to your household.");
        }

        if (this.people.filter(x => x.child == false).length <= 0) {
            this.errors.push("At least one adult must be added to your household.");
        }

        let validationResults = await Promise.all(this.people.map(p => this.validation.validate({ object: p })));

        validationResults.filter(f => !f.valid).forEach(r => {
            this.errors.push(`${r.instruction.object.displayName}: At least one field is not valid.`);
        });

        if (this.errors.length) {
            return;
        }

        sessionStorage.setItem(`Household_${this.hid}`, JSON.stringify({
            people: this.people,
            id: this.hid
        }));
    }

    async backClicked() {
        this.router.navigateToRoute("family", { id: this.hid } );
    }

    async submitClicked() {
        let result = await this.http.fetch("/Home/CompleteRegistration", {
            credentials: 'same-origin',
            method: "post",
            body: json({
                householdID: this.hid,
                isNew: this.new,
                people: this.people
            })
        });

        if (result.ok) {
            sessionStorage.removeItem(`Household_${this.hid}`);
        } else {
            let msg = await result.text();
            this.errors.push(msg);
        }
    }
}