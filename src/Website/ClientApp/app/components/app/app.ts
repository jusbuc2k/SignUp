import { Aurelia, PLATFORM } from 'aurelia-framework';
import { Router, RouterConfiguration } from 'aurelia-router';

export class App {
    router: Router;

    configureRouter(config: RouterConfiguration, router: Router) {
        config.title = 'Aurelia';
        config.map([
            {
                route: [ '', 'home' ],
                name: 'home',
                settings: { icon: 'home' },
                moduleId: PLATFORM.moduleName('../home/home'),
                nav: true,
                title: 'Home'
            },

            { route: 'start', name: 'start', moduleId: PLATFORM.moduleName("../login/start"), title: "First Time" },
            { route: 'first-time', name: 'first-time', moduleId: PLATFORM.moduleName("../login/FirstTime"), title: "First Time" },
            { route: 'family/:id', name: 'family', moduleId: PLATFORM.moduleName("../login/Family"), title: "Household" },
            { route: 'review', name: 'review', moduleId: PLATFORM.moduleName("../login/Review"), title: "Review" }

        ]);

        this.router = router;
    }
}
