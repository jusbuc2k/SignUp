import { HttpClient , json } from 'aurelia-fetch-client';
import { autoinject } from 'aurelia-framework';
import { Router } from "aurelia-router";
import { globalState } from "../../GlobalState";
import { Person } from "./Person";
import * as moment from "moment";
import { EventAggregator, Subscription } from "aurelia-event-aggregator";
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

        this.subscriptions.push(eventAggregator.subscribe("Person_Updated", (data) => {
            this.submitNewPersonClicked();
        }));

        this.subscriptions.push(eventAggregator.subscribe("Person_Cancel", (data) => {
            this.cancelClicked();
        }));
    }

    subscriptions: Subscription[] = [];
    errorMessage: string = "";
    inProgress: boolean = false;

    emailAddress: string = "";
    showEmailSearchForm: boolean = true;
    showFirstTimeForm: boolean = false;
    showLoginForm: boolean = false;
    newPerson: Person;

    protected tokenID: string;
    token: string;

    activate(params) {
        if (!this.eventModel.event) {
            this.router.navigate("#/");
        }
    }

    deactivate() {
        let sub;
        while (sub = this.subscriptions.pop()) {
            sub.dispose();
        }
    }

    async findClicked() {
        if (this.inProgress) {
            return;
        }

        this.errorMessage = "";
        this.inProgress = true;

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
                await this.loadAndStart();
            } else {
                this.tokenID = values.tokenID;
                this.showEmailSearchForm = false;
                this.showLoginForm = true;
            }       
        } else if (result.status == 404) {
            this.newPerson = new Person();
            this.newPerson.child = false;
            this.newPerson.emailAddress = this.emailAddress;
            this.newPerson.isPrimaryContact = true;

            this.showEmailSearchForm = false;
            this.showFirstTimeForm = true;
        } else {
            this.errorMessage = "Sorry, but we were unable to lookup the e-mail address you provided at this time. " + ErrorSuffix;
        }

        this.inProgress = false;
    }

    cancelClicked() {
        this.showEmailSearchForm = true;
        this.showLoginForm = false;
        this.showFirstTimeForm = false;
    }

    async loadAndStart() {
        let result = await this.http.fetch(`/api/GetOrCreateHouse`, {
            credentials: 'same-origin',
            method: "post"
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
        if (this.inProgress) {
            return;
        }

        this.errorMessage = "";
        this.inProgress = true;
        
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

            await this.loadAndStart();
            this.inProgress = false;
        } else {
            this.errorMessage = "Sorry, we couldn't verify the code you entered was correct. Please try again or go back and start over.";
            this.token = "";       
            this.inProgress = false;
        }
    }

    submitNewPersonClicked() {
        this.eventModel.house = {
            people: [
                this.newPerson
            ]
        };

        this.router.navigateToRoute("family");
    }
}

interface ILoginTokenResponse {
    TokenID: string;
}
