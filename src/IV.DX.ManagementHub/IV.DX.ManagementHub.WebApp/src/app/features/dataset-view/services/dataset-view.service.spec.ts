import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { of } from 'rxjs';

import { InstancesService } from '@core/instances/instances.service';
import { DatasetViewService } from './dataset-view.service';

const COMPONENT_ID = '018fa54b-aac6-7d9a-9c81-ab6bb4df37a7';
const QUERY_ID = '018fa54b-58be-7551-bd5b-e4d9c6c922c4';
/** Requests are instance-scoped; the real key comes from the route. */
const API_BASE = '/api/i/Own';

/**
 * Lets the pending HTTP promise resolve, then runs effects so the dependent
 * resource can issue its own request.
 */
async function settle(): Promise<void> {
  await new Promise((resolve) => setTimeout(resolve, 0));
  TestBed.tick();
}

function setup() {
  TestBed.configureTestingModule({
    providers: [
      provideHttpClient(),
      provideHttpClientTesting(),
      {
        provide: ActivatedRoute,
        useValue: { paramMap: of(convertToParamMap({ componentId: COMPONENT_ID })) },
      },
      // Stubbed so the test does not need a router; only the prefix matters here.
      { provide: InstancesService, useValue: { apiBase: () => API_BASE } },
      DatasetViewService,
    ],
  });

  return {
    service: TestBed.inject(DatasetViewService),
    http: TestBed.inject(HttpTestingController),
  };
}

const unitPayload = (overrides: Record<string, unknown> = {}) => ({
  Meta: { Type: 'DXPDataSetViewUnit' },
  Data: {
    Items: [{ Id: COMPONENT_ID, Name: 'DXEnum DataSetView', DXQuery: QUERY_ID, ...overrides }],
  },
});

describe('DatasetViewService', () => {
  it('loads the definition and then the rows of its query', async () => {
    const { service, http } = setup();
    await settle();

    http.expectOne(`${API_BASE}/DXPDataSetViewUnit/${COMPONENT_ID}`).flush(unitPayload());
    await settle();

    expect(service.definition()?.queryId).toBe(QUERY_ID);

    http.expectOne(`${API_BASE}/DXQueryResult/${QUERY_ID}`).flush({
      QueryDefinition: [{ Name: 'Name', Expression: 'Name', Order: 20 }],
      Content: {
        Meta: { Type: 'DXEnumDefinitionUnit' },
        Data: { Items: [{ Id: 'r1', Name: 'A' }] },
      },
    });
    await settle();

    expect(service.table().rows.length).toBe(1);
    expect(service.isLoading()).toBe(false);
    http.verify();
  });

  it('stops loading and reports an error when the definition is missing', async () => {
    const { service, http } = setup();
    await settle();

    http
      .expectOne(`${API_BASE}/DXPDataSetViewUnit/${COMPONENT_ID}`)
      .flush({ title: 'Not Found', status: 404 }, { status: 404, statusText: 'Not Found' });
    await settle();

    expect(service.error()).toBeTruthy();
    expect(service.isLoading()).toBe(false);
    expect(service.isUnresolved()).toBe(false);

    // Reading the exposed signals must stay safe: `resource.value()` throws a
    // ResourceValueError while the resource is in an error state, which used to
    // take the template down with it and leave the screen on its spinner.
    expect(() => service.definition()).not.toThrow();
    expect(() => service.table()).not.toThrow();
    expect(service.definition()).toBeNull();
    expect(service.table().rows).toEqual([]);

    http.verify();
  });

  it('marks a definition without a query as unresolved and makes no second request', async () => {
    const { service, http } = setup();
    await settle();

    http
      .expectOne(`${API_BASE}/DXPDataSetViewUnit/${COMPONENT_ID}`)
      .flush(unitPayload({ DXQuery: null }));
    await settle();

    expect(service.definition()).toBeNull();
    expect(service.isUnresolved()).toBe(true);
    expect(service.isLoading()).toBe(false);
    http.verify();
  });
});
