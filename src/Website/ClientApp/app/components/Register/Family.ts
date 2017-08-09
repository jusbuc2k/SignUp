import { HttpClient } from 'aurelia-fetch-client';
import { autoinject } from 'aurelia-framework';
import { Person } from "./Person";
import { ValidationControllerFactory, ValidationController } from "aurelia-validation";
import { EventAggregator } from "aurelia-event-aggregator";
import { Router } from "aurelia-router";
import { DataStore } from "../../DataStore";
import { EventModel } from "../home/event";

@autoinject()
export class FamilyModel {
    constructor(
        protected eventModel: EventModel,
        protected http: HttpClient,
        protected router: Router,
        protected validationControllerFactory: ValidationControllerFactory,
        protected eventAggregator: EventAggregator) {

        this.validation = validationControllerFactory.createForCurrentScope();

        eventAggregator.subscribe("Person_Updated", (data) => {
            if (this.people.indexOf(data) < 0) {
                this.people.push(data);
                //this.eventModel.house.people.push(data);
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

    protected validation: ValidationController;

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
        if (this.eventModel.house) {
            this.people = this.eventModel.house.people;
        } else {
            this.router.navigateToRoute("start");
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

        this.eventModel.house.people = this.people;
                
        this.router.navigateToRoute("review");
    }

    async findClicked() {
        
    }
}