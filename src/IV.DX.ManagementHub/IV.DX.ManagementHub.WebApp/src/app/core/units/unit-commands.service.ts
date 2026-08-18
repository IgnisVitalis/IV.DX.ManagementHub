import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

import { InstancesService } from '@core/instances/instances.service';

/** Writes against a DX unit: save, delete, export. */
@Injectable({ providedIn: 'root' })
export class UnitCommands {
  private readonly http = inject(HttpClient);
  private readonly instances = inject(InstancesService);

  /** Every write is scoped to the instance currently in the URL. */
  private base(): string {
    const base = this.instances.apiBase();

    if (base === undefined) {
      throw new Error('Не выбран инстанс DX.');
    }

    return base;
  }

  private itemUrl(typeName: string, id: string): string {
    return `${this.base()}/${typeName}/${id}`;
  }

  /** Creates a record. The API generates the id and answers 201 with it. */
  async create(typeName: string, payload: unknown): Promise<string> {
    const created = await firstValueFrom(
      this.http.post<{ id: string }>(`${this.base()}/${typeName}`, payload),
    );

    return created.id;
  }

  /** Saves a patched record payload. The API answers 204. */
  async update(typeName: string, id: string, payload: unknown): Promise<void> {
    await firstValueFrom(this.http.put(this.itemUrl(typeName, id), payload));
  }

  async delete(typeName: string, id: string): Promise<void> {
    await firstValueFrom(this.http.delete(this.itemUrl(typeName, id)));
  }

  /**
   * Deletes several records.
   *
   * Sequentially, like the Blazor confirmation dialog: the API has no bulk
   * delete, and firing them in parallel would make a partial failure harder to
   * report than it already is.
   */
  async deleteMany(typeName: string, ids: readonly string[]): Promise<void> {
    for (const id of ids) {
      await this.delete(typeName, id);
    }
  }

  /**
   * Downloads one record as formatted JSON, keeping the file name the Blazor
   * version used so exports stay interchangeable with existing migrations.
   */
  async export(typeName: string, ids: readonly string[]): Promise<void> {
    const record =
      ids.length === 1
        ? await firstValueFrom(this.http.get(this.itemUrl(typeName, ids[0])))
        : await firstValueFrom(this.http.post(`${this.base()}/${typeName}/by-ids`, ids));

    const blob = new Blob([JSON.stringify(record, null, 2)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);

    try {
      const link = document.createElement('a');
      link.href = url;
      link.download = `01_01_0001_UIUX_${typeName}.dx`;
      link.click();
    } finally {
      URL.revokeObjectURL(url);
    }
  }
}
