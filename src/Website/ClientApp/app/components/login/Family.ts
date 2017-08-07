import { HttpClient } from 'aurelia-fetch-client';
import { autoinject } from 'aurelia-framework';
import { Person } from "./Person";
import { ValidationControllerFactory, ValidationController } from "aurelia-validation";
import { EventAggregator } from "aurelia-event-aggregator";
import { Router } from "aurelia-router";

@autoinject()
export class FamilyModel {
    constructor(http: HttpClient, router: Router, validationControllerFactory: ValidationControllerFactory, eventAggregator: EventAggregator) {
        this.http = http;
        this.router = router;
        this.validation = validationControllerFactory.createForCurrentScope();

        eventAggregator.subscribe("Person_Updated", (data) => {
            if (this.people.indexOf(data) < 0) {
                this.people.push(data);
            }            

            if (data.isPrimaryContact) {
                this.people.forEach(p => {
                    if (p !== data) {
                        p.isPrimaryContact = false;
                    }
                });
            }

            this.selectedPerson = null;
        });

        eventAggregator.subscribe("Person_Cancel", (data) => {
            this.selectedPerson = null;
        });
    }

    protected http: HttpClient;
    protected validation: ValidationController;
    protected router: Router;

    hid: string;
    new: boolean;
    people: Person[] = [];
    selectedPerson: Person = null;
    errors: string[] = [];

    selectPerson(person) {
        this.selectedPerson = person;
    }

    addPerson(isChild: boolean) {
        let p = new Person();
        p.child = isChild;
        this.selectedPerson = p;
    }

    removeSelected() {
        if (this.selectedPerson) {
            let selectedIndex = this.people.indexOf(this.selectedPerson);
            this.people.splice(selectedIndex, 1);
            this.selectedPerson = this.people[this.people.length - 1];
        }
    }

    async activate(params) {
        let house = JSON.parse(sessionStorage.getItem(`Household_${params.id}`));
        this.people = house.people.map(p => Object.assign(new Person(), p));
        this.hid = house.id;
        this.new = house.new;

        if (this.people.length === 1) {
            this.selectedPerson = this.people[0];
        }
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

        this.router.navigateToRoute("review", { id: this.hid });
    }

    async findClicked() {
        
    }
}