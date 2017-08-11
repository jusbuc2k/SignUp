import { HttpClient , json } from 'aurelia-fetch-client';
import { autoinject } from 'aurelia-framework';
import { Router } from "aurelia-router";
import { globalState } from "../../GlobalState";
import { Person } from "./Person";
import { newGuid } from "../../Guid";
import * as moment from "moment";
import { EventAggregator } from "aurelia-event-aggregator";
import { DataStore } from "../../DataStore";
import { EventModel } from "../home/event";

const ErrorSuffix = "Please try again later or contact support for assistance.";

@autoinject()
export class StartModel {
    constructor(
        protected eventModel: EventModel,
        protected http: HttpClient,
        protected router: Router,
        protected eventAggregator: EventAggregator) {

        eventAggregator.subscribe("Person_Updated", (data) => {
            this.submitNewPersonClicked();
        });

        eventAggregator.subscribe("Person_Cancel", (data) => {
            this.cancelClicked();
        });
    }

    errorMessage: string = "";
    emailAddress: string = "";
    showEmailSearchForm: boolean = true;
    showFirstTimeForm: boolean = false;
    showLoginForm: boolean = false;
    newPerson: Person;

    protected tokenID: string;
    token: string;

    activate(params) {

    }

    async findClicked() {
        this.errorMessage = "";

        let result = await this.http.fetch('/api/FindEmail', {
            credentials: 'same-origin',
            method: "post",
            body: json({
                EmailAddress: this.emailAddress
            })
        });

        if (result.ok) {
            let values = await result.json();

            if (values.verified) {
                await this.loadAndStart(values.householdID);
            } else {
                this.tokenID = values.tokenID;
                this.showEmailSearchForm = false;
                this.showLoginForm = true;
            }            
        } else if (result.status == 404) {
            this.newPerson = new Person();
            this.newPerson.id = newGuid();
            this.newPerson.child = false;
            this.newPerson.emailAddress = this.emailAddress;
            this.newPerson.isPrimaryContact = true;

            this.showEmailSearchForm = false;
            this.showFirstTimeForm = true;
        } else {
            this.errorMessage = "Sorry, but we were unable to lookup the e-mail address you provided at this time. " + ErrorSuffix;
        }
    }

    cancelClicked() {
        this.showEmailSearchForm = true;
        this.showLoginForm = false;
        this.showFirstTimeForm = false;
    }

    async loadAndStart(hid: string) {
        let result = await this.http.fetch(`/api/Household/${hid}`, {
            credentials: 'same-origin'
        });

        if (!result.ok) {
            this.errorMessage = "Sorry, but were unable to load your household. " + ErrorSuffix;
        }

        this.eventModel.house = await result.json();

        this.eventModel.house.people = this.eventModel.house.people.map(p => {
            return Object.assign(new Person(), p, {
                birthDate: p.birthDate ? moment(p.birthDate, "YYYY-MM-DD").format("M/D/YYYY") : null
            });
        });

        this.router.navigateToRoute("family");
    }

    async verifyCodeClicked() {
        this.errorMessage = "";

        let result = await this.http.fetch('/api/VerifyLoginToken', {
            credentials: 'same-origin',
            method: "post",
            body: json({
                TokenID: this.tokenID,
                Token: this.token
            })
        });

        if (result.ok) {
            let data = await result.json();

            this.token = "";

            await this.loadAndStart(data.householdID);
        } else {
            this.errorMessage = "Sorry, we couldn't verify the code you entered was correct. Please try again or go back and start over.";
            this.token = "";            
        }
    }

    submitNewPersonClicked() {
        let hid  = newGuid();

        this.eventModel.house = {
            id: hid,
            people: [
                this.newPerson
            ],
            new: true
        };

        this.router.navigateToRoute("family");
    }
}

interface ILoginTokenResponse {
    TokenID: string;
}
