import { HttpClient , json } from 'aurelia-fetch-client';
import { autoinject } from 'aurelia-framework';
import { Router } from "aurelia-router";
import { globalState } from "../../GlobalState";
import { Person } from "./Person";
import { newGuid } from "../../Guid";
import * as moment from "moment";

@autoinject()
export class StartModel {
    constructor(http: HttpClient, router: Router) {
        this.http = http;
        this.router = router;
    }

    protected http: HttpClient;
    protected router: Router;

    emailAddress: string = "";
    showEmailSearchForm: boolean = true;
    showFirstTimeForm: boolean = false;
    showLoginForm: boolean = false;
    newPerson: Person;

    protected tokenID: string;
    token: string;

    async findClicked() {
        let result = await this.http.fetch('/Home/FindEmail', {
            credentials: 'same-origin',
            method: "post",
            body: json({
                EmailAddress: this.emailAddress
            })
        });

        if (result.ok) {
            let values = await result.json();

            this.tokenID = values.tokenID;

            this.showLoginForm = true;
        } else if (result.status == 404) {
            this.newPerson = new Person();
            this.newPerson.id = newGuid();
            this.newPerson.child = false;
            this.newPerson.emailAddress = this.emailAddress;
            this.newPerson.isPrimaryContact = true;

            this.showEmailSearchForm = false;
            this.showFirstTimeForm = true;
        }
    }

    async confirmClicked() {
        let result = await this.http.fetch('/Home/VerifyLoginToken', {
            credentials: 'same-origin',
            method: "post",
            body: json({
                TokenID: this.tokenID,
                Token: this.token
            })
        });

        if (result.ok) {
            let data = await result.json();

            result = await this.http.fetch(`/Home/GetHousehold/${data.householdID}`, {
                credentials: 'same-origin'
            });

            let house = await result.json();

            house.people.forEach(p => {
                if (p.birthDate) {
                    p.birthDate = moment(p.birthDate, "YYYY-MM-DD").format("M/D/YYYY");
                }
            });

            sessionStorage.setItem(`Household_${house.householdID}`, JSON.stringify({
                id: house.householdID,
                people: house.people
            }));

            this.router.navigateToRoute("family", { id: data.householdID });
        } else {
            console.error("Invalid token!");
        }
    }

    submitNewPersonClicked() {
        let hid = newGuid();

        sessionStorage.setItem(`Household_${hid}`, JSON.stringify({
            id: hid,
            new: true,
            people: [
                this.newPerson
            ]
        }));

        this.router.navigateToRoute("family", { id: hid, new: 1 });
    }
}

interface ILoginTokenResponse {
    TokenID: string;
}
