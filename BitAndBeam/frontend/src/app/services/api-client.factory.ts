import { Injectable } from '@angular/core';
import { Configuration } from '../../api';
import { ConfigService } from '../config.service';
import { SessionService } from './session.service';

@Injectable({ providedIn: 'root' })
export class ApiClientFactory {
  constructor(
    private config: ConfigService,
    private session: SessionService,
  ) {}

  create<T>(type: new (cfg: Configuration) => T): T {
    const token = this.session.getToken();

    const config = new Configuration({
      basePath: this.config.apiUrl,

      // ✅ Axios clients require headers to be explicitly passed
      baseOptions: {
        headers: {
          Authorization: token ? `Bearer ${token}` : '',
        },
      },
    });
    return new type(config);
  }
}
