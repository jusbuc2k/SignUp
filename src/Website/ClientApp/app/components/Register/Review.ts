import { HttpClient, json } from 'aurelia-fetch-client';
import { autoinject } from 'aurelia-framework';
import { Person } from "./Person";
import { ValidationControllerFactory, ValidationController } from "aurelia-validation";
import { EventAggregator } from "aurelia-event-aggregator";
import { Router } from "aurelia-router";
import { EventModel } from "../home/event";

interface IFeeSchedule {
    maxAge: number;
    maxGrade: number;
    child: boolean;
    cost: number;
    group: string
}

const SaveErrorMessage = "Sorry, but we could not save your registration at this time. Please try again later or contact support.";
const AtLeastOnePersonSelectedErrorMessage = "At least one person must be selected to register.";

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

    validation: ValidationController;
    people: Person[] = [];
    errors: string[] = [];
    inProgress: boolean = false;
    eventFeesNotice: string;

    get totalCost(): number {
        return this.people.reduce((sum, cur) => {
            if (cur["selected"] && cur["fee"]) {
                return sum + cur["fee"]["cost"];
            } else {
                return sum;
            }
        }, 0);
    }

    activate(params) {
        if (!this.eventModel.house) {
            this.router.navigate(`#/event/${this.eventModel.event.eventID}`);
            return;
        }

        // Load household people and all applicable fees / group assignments
        //TODO: this is a mess right now, need to re-think these rules. There's no way
        // to set a minimum age/grade and set a cutoff date for the age (e.g. age 3 by Aug 1)
        this.people = this.eventModel.house.people.map(p => {
            let fee = this.eventModel.event.fees.find(f =>
                f.child === p.child
                && (f.gender === p.gender || f.gender === "*")
                && f.maxAge >= p.age
                && (p.grade == null || p.grade == "" || f.maxGrade >= parseInt(p.grade, 10)))

            return Object.assign(new Person(), {
                eligable: fee != null,
                selected: false
            }, p, {
                fee: fee
            });
        });
    }

    backClicked() {
        this.router.navigateToRoute("family");
    }

    async submitClicked() {
        if (this.inProgress) {
            return;
        }

        this.errors.splice(0, this.errors.length);

        if (this.people.every(x => !x["selected"])) {
            this.errors.push(AtLeastOnePersonSelectedErrorMessage);
            return;
        }

        this.inProgress = true;

        try {
            // save the registration, family info, etc.

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
                this.errors.push(SaveErrorMessage);
            }

            this.inProgress = false;
        } catch (e) {
            this.inProgress = false;
            this.errors.push(SaveErrorMessage);
        }
    }
}