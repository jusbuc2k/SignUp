import { HttpClient, json } from 'aurelia-fetch-client';
import { autoinject } from 'aurelia-framework';
import { Person } from "./Person";
import { ValidationControllerFactory, ValidationController } from "aurelia-validation";
import { EventAggregator } from "aurelia-event-aggregator";
import { Router } from "aurelia-router";
import { EventModel } from "../home/event";
import * as moment from "moment";

interface IFeeGroup {
    order: number;
    minAge: number;
    maxAge: number;
    ageUnit: string;
    ageCutoff: string;
    minGrade: number;
    maxGrade: number;
    gender: string;
    child: boolean;
    cost: number;
    group: string;
    applyAgeFilter: boolean;
    applyGradeFilter: boolean;
}

function getFeeGroup(fees: IFeeGroup[], person: Person) {
    return fees.find(fee => {
        if (fee.child !== person.child) {
            return false;
        }

        if (fee.gender !== "*" && person.gender !== fee.gender) {
            return false;
        }

        // check grade rules
        if (fee.applyGradeFilter && (fee.minGrade >= -1 || fee.maxGrade >= -1)) {
            if (person.grade == null) {
                return false;
            } else if (person.grade > fee.maxGrade || person.grade < fee.minGrade) {
                return false;
            }
        }

        // check age rules.
        if (fee.applyAgeFilter && (fee.minAge || fee.maxAge)) {
            if (person.birthDate == null || person.birthDate === "") {
                return false;
            } else {
                let age = (fee.ageCutoff == null) ? person.age : moment(fee.ageCutoff, "YYYY-MM-DD").diff(moment(person.birthDate, "M/D/YYYY"), "years");

                if (age > fee.maxAge || age < fee.minAge) {
                    return false;
                }
            }
        }       

        return true;
    });
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

        let getFeeGroupFn = getFeeGroup.bind(null, this.eventModel.event.fees);

        // Load household people and all applicable fees / group assignments
        //TODO: this is a mess right now, need to re-think these rules. There's no way
        // to set a minimum age/grade and set a cutoff date for the age (e.g. age 3 by Aug 1)
        this.people = this.eventModel.house.people.map(p => {
            let fee = getFeeGroupFn(p);

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