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
            this.router.navigateToRoute("start");
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

        //if (this.fees.length > 0) {
        //    this.people.forEach(p => {
        //        let fee = this.fees.find(f => f.child === p.child && f.maxAge >= p.age && f.maxGrade >= parseInt(p.grade, 10));

        //        if (fee) {
        //            p["cost"] = fee.cost;
        //            p["group"] = fee.group;
        //            this.totalCost += fee.cost;
        //        }
        //    });
        //}

        //this.hid = house.id;
    }

    async backClicked() {
        this.router.navigateToRoute("family");
    }

    async submitClicked() {
        this.errors.splice(0, this.errors.length);
        
        if (this.people.every(x => !x["selected"])) {
            this.errors.push("At least one person must be selected to register.");
            return;
        }
        
        let result = await this.http.fetch("/api/CompleteRegistration", {
            credentials: 'same-origin',
            method: "post",
            body: json(Object.assign({}, this.eventModel.house, {
                people: this.people
            }))
        });

        if (result.ok) {
            this.eventModel.house = null;
            this.router.navigate("/home");
        } else {
            this.errors.push("Sorry, but we could not save your registration at this time. Please try again later or contact support.");
        }
    }
}