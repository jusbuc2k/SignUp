import { HttpClient } from 'aurelia-fetch-client';
import { autoinject } from 'aurelia-framework';
import { Person } from "./Person";
import { Router } from "aurelia-router";
import { newGuid } from "../../Guid";

@autoinject()
export class FirstTimeModel {

    constructor(http: HttpClient, router: Router) {
        this.http = http;
        this.router = router;
        this.data = new Person();
        this.data.id = newGuid();
        this.data.child = false;
    }

    protected http: HttpClient;
    protected router: Router;

    data: Person;

    submitClicked() {
        sessionStorage.setItem(`Person_${this.data.id}`, JSON.stringify(this.data));
        this.router.navigateToRoute("Family", { person: this.data.id });
    }
}
