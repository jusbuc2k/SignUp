import { HttpClient } from 'aurelia-fetch-client';
import { autoinject } from 'aurelia-framework';
import { Person } from "./Person";
import { ValidationControllerFactory, ValidationController } from "aurelia-validation";
import { EventAggregator, Subscription } from "aurelia-event-aggregator";
import { Router } from "aurelia-router";
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

        this.subscriptions.push(eventAggregator.subscribe("Person_Updated", (data) => {
            if (this.people.indexOf(data) < 0) {
                this.people.push(data);
            }

            if (data.isPrimaryContact) {
                this.people.forEach(p => {
                    if (p !== data && data["cascadeAddress"]) {
                        p.street = data.street;
                        p.city = data.city;
                        p.state = data.state;
                        p.zip = data.zip;
                    }

                    if (p !== data) {
                        p.isPrimaryContact = false;
                    }
                });
            }

            this.selectedPerson = null;
        }));

        this.subscriptions.push(eventAggregator.subscribe("Person_Cancel", (data) => {
            this.selectedPerson = null;
        }));

        this.subscriptions.push(eventAggregator.subscribe("Person_Deleted", (data) => {
            let idx = this.people.indexOf(data);
            if (idx >= 0) {
                this.people.splice(idx, 1);
            }
            this.selectedPerson = null;
        }));
    }

    protected validation: ValidationController;
    protected subscriptions: Subscription[] = [];

    people: Person[] = [];
    selectedPerson: Person = null;
    errors: string[] = [];

    selectPerson(person) {
        this.selectedPerson = person;
    }

    addPerson(isChild: boolean) {
        let primary = this.people.find(x => x.isPrimaryContact);
        let p = new Person();

        p.child = isChild;

        if (primary) {
            p.lastName = primary.lastName;
            p.street = primary.street;
            p.city = primary.city;
            p.state = primary.state;
            p.zip = primary.zip;
        }

        this.selectedPerson = p;
    }

    removeSelected() {
        if (this.selectedPerson) {
            let selectedIndex = this.people.indexOf(this.selectedPerson);
            this.people.splice(selectedIndex, 1);
            this.selectedPerson = this.people[this.people.length - 1];
        }
    }

    activate(params) {
        if (this.eventModel.house) {
            this.people = this.eventModel.house.people;
        } else {
            this.router.navigate(`#/event/${this.eventModel.event.eventID}`);
        }
    }

    deactivate() {
        let sub;
        while (sub = this.subscriptions.pop()) {
            sub.dispose();
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

}