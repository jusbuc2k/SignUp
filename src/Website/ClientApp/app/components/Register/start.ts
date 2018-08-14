import { HttpClient , json } from 'aurelia-fetch-client';
import { autoinject } from 'aurelia-framework';
import { Router } from "aurelia-router";
import { Person } from "./Person";
import * as moment from "moment";
import { EventAggregator, Subscription } from "aurelia-event-aggregator";
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

    tokenID: string;
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

    // Action taken when searching for a user by e-mail address on the first screen
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

    // Get or create a user's household and load the family screen
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
                grade: p.child ? -2 : p.grade,
                birthDate: p.birthDate ? moment(p.birthDate, "YYYY-MM-DD").format("M/D/YYYY") : null
            });
        });

        this.router.navigateToRoute("family");
    }

    // When the suer clicks submit on the code verification screen
    async verifyCodeClicked() {
        if (this.inProgress) {
            return;
        }

        this.errorMessage = "";
        this.inProgress = true;

        // Validate the verification code sent via e-mail and sign the user in if it's valid
        let result = await this.http.fetch('/api/VerifyLoginToken', {
            credentials: 'same-origin',
            method: "post",
            body: json({
                TokenID: this.tokenID,
                Token: this.token
            })
        });

        this.token = "";       

        if (result.ok) {
            let data = await result.json();

            await this.loadAndStart();

            this.inProgress = false;
        } else {
            this.errorMessage = "Sorry, we couldn't verify the code you entered was correct. Please try again or go back and start over.";
            this.inProgress = false;
        }
    }

    // Setup a new house with the single new person and launch the family screen
    submitNewPersonClicked() {
        this.eventModel.house = {
            people: [
                this.newPerson
            ]
        };

        this.router.navigateToRoute("family");
    }
}
