import { ChangeDetectorRef, Component, ViewChild } from '@angular/core';

import { ChangeDetectionStrategy } from '@angular/core';
import { endWith } from 'rxjs';
import * as tus from 'tus-js-client';
@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrl: './app.component.css',
})
export class AppComponent {
  title = 'UploadFilesClient';

  Uplaod(event: any) {
    var myFile: File = event.target.files[0];

    var upload = new tus.Upload(myFile, {
      endpoint: 'https://localhost:7264/files/',
      retryDelays: [0, 3000, 5000, 10000, 20000],
      metadata: {
        filename: myFile.name,
        filetype: myFile.type,
      },
      onError: function (error) {
        console.log('Failed because: ' + error);
      },
      onProgress: function (bytesUploaded, bytesTotal) {
        var percentage = ((bytesUploaded / bytesTotal) * 100).toFixed(2);
        console.log(bytesUploaded, bytesTotal, percentage + '%');
      },
      onSuccess: function () {
        console.log('Download %s from %s', upload.file, upload.url);
      },
      onChunkComplete(chunkSize, bytesAccepted, bytesTotal) {
        console.log('-------');
        console.log('chunkSize', chunkSize);
        console.log('bytesAccepted', bytesAccepted);
        console.log('bytesTotal', bytesTotal);
      },
    });
    upload.findPreviousUploads().then(function (previousUploads) {
      // Found previous uploads so we select the first one.
      if (previousUploads.length) {
        upload.resumeFromPreviousUpload(previousUploads[0]);
      }

      // Start the upload
      upload.start();
    });
  }
}
