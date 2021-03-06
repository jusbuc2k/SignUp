import { Aurelia, PLATFORM } from 'aurelia-framework';
import { Router, RouterConfiguration } from 'aurelia-router';

declare var $Environment: any;

export class App {
    router: Router;

    siteName: string = $Environment.siteName;

    configureRouter(config: RouterConfiguration, router: Router) {
        config.title = $Environment.siteName;
        config.map([
            {
                route: [ '', 'home' ],
                name: 'home',
                settings: { icon: 'home' },
                moduleId: PLATFORM.moduleName('../home/home'),
                nav: true,
                title: 'Home'
            },

            { route: 'event/:id', name: 'event', moduleId: PLATFORM.moduleName("../home/event"), title: "Event Registration" }
        ]);

        this.router = router;
    }
}
