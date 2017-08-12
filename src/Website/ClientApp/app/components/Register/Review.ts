import { HttpClient, json } from 'aurelia-fetch-client';
import { autoinject } from 'aurelia-framework';
import { Person } from "./Person";
import { ValidationControllerFactory, ValidationController } from "aurelia-validation";
import { EventAggregator } from "aurelia-event-aggregator";
import { Router } from "aurelia-router";
import { DataStore } from "../../DataStore";
import { EventModel } from "../home/event";

interface IFeeSchedule {
    maxAge: number;
    maxGrade: number;
    child: boolean;
    cost: number;
    group: string
}

@autoinject()
export class FamilyModel {
    constructor(
        protected eventModel: EventModel,
        protected http: HttpClient,
        protected router: Router,
        protected validationControllerFactory: ValidationControllerFactory,
        protected eventAggregator: EventAggregator) {
        this.validation = validationControllerFactory.createForCurrentScope();
    }

    protected validation: ValidationController;

    people: Person[] = [];

    errors: string[] = [];
    inProgress: boolean = false;

    get totalCost(): number {
        return this.people.reduce((sum, cur) => {
            if (cur["selected"] && cur["fee"]) {
                return sum + cur["fee"]["cost"];
            } else {
                return sum;
            }
        }, 0);
    }

    eventFeesNotice: string;

    activate(params) {
        if (!this.eventModel.house) {
            this.router.navigate(`#/event/${this.eventModel.event.eventID}`);
            return;
        }

        this.people = this.eventModel.house.people.map(p => {
            let fee = this.eventModel.event.fees.find(f =>
                f.child === p.child
                && (f.gender === p.gender || f.gender === "*")
                && f.maxAge >= p.age
                && f.maxGrade >= parseInt(p.grade, 10));

            return Object.assign(new Person(), {
                eligable: fee != null,
                selected: false
            }, p, {
                fee: fee
            });
        });
    }

    async backClicked() {
        this.router.navigateToRoute("family");
    }

    async submitClicked() {
        if (this.inProgress) {
            return;
        }

        this.errors.splice(0, this.errors.length);
        this.inProgress = true;

        if (this.people.every(x => !x["selected"])) {
            this.errors.push("At least one person must be selected to register.");
            return;
        }

        try {
            let result = await this.http.fetch("/api/CompleteRegistration", {
                credentials: 'same-origin',
                method: "post",
                body: json(Object.assign({}, this.eventModel.house, {
                    people: this.people,
                    eventID: this.eventModel.event.eventID
                }))
            });

            if (result.ok) {
                this.eventModel.house = null;
                this.router.navigateToRoute("confirm");
            } else {
                this.errors.push("Sorry, but we could not save your registration at this time. Please try again later or contact support.");
            }

            this.inProgress = false;
        } catch (e) {
            this.inProgress = false;
            this.errors.push("Sorry, but we could not save your registration at this time. Please try again later or contact support.");
        }
    }
}