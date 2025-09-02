//
//  NPMediaServices.mm
//  Native Plugins
//
//  Created by Ayyappa on 26/06/24.
//  Copyright (c) 2024 Voxel Busters Interactive LLP. All rights reserved.
//

#import "NPMediaServices.h"
#import "NPKit.h"
#import "UIViewController+Presentation.h"
#import "UnityInterface.h"


#import <PhotosUI/PHPicker.h>
#import <UniformTypeIdentifiers/UTType.h>
#import <UniformTypeIdentifiers/UTCoreTypes.h>


static NPMediaServices*  _sharedInstance;


@implementation MediaContentSelectionOptions
@end

@implementation MediaContentCaptureOptions
@end

@implementation MediaContentSaveOptions
@end

@interface NPMediaServices ()
@property (nonatomic, copy) SelectMediaContentCallback selectMediaContentCallback;
@property (nonatomic, copy) CaptureMediaContentCallback captureMediaContentCallback;
@property (nonatomic, copy) SaveMediaContentCallback saveMediaContentCallback;

@end

@implementation NPMediaServices
@synthesize selectMediaContentCallback;
@synthesize captureMediaContentCallback;
@synthesize saveMediaContentCallback;

+ (NPMediaServices*)sharedInstance
{
    if (!_sharedInstance)
    {
        _sharedInstance = [[NPMediaServices alloc] init];
    }
    return _sharedInstance;
}

-(void) selectMediaContent:(MediaContentSelectionOptions*) options withCallback:(SelectMediaContentCallback) callback
{
    NSString *mimeType = options.allowedMimeType;

    self.selectMediaContentCallback = callback;

    if([self isImageMime:mimeType] || [self isVideoMime:mimeType])
    {
        [self pickWithPhotoPicker:mimeType options:options];
    }
    else
    {
        [self pickWithDocumentPicker:options];
    }
}

-(void) captureMediaContent:(MediaContentCaptureOptions*) options withCallback:(CaptureMediaContentCallback) callback
{
    if(options.captureType == MediaContentCaptureTypeImage || options.captureType == MediaContentCaptureTypeVideo)
    {
        [AVCaptureDevice requestAccessForMediaType:AVMediaTypeVideo completionHandler:^(BOOL granted) {
            // send callback
            AVAuthorizationStatus   status  = [AVCaptureDevice authorizationStatusForMediaType:AVMediaTypeVideo];
            
            if(status == AVAuthorizationStatusAuthorized)
            {
                dispatch_async(dispatch_get_main_queue(), ^{
                    UIImagePickerController*    picker  = [[UIImagePickerController alloc] init];
                    picker.delegate                     = self;
                    picker.sourceType                   = UIImagePickerControllerSourceTypeCamera;
                    NSArray* types = @[options.captureType == MediaContentCaptureTypeImage ? UTTypeImage.identifier : UTTypeMovie.identifier];
                    picker.mediaTypes                   = types;
                    self.captureMediaContentCallback    = callback;
                    
                    [self presentPicker:picker];
                });
            }
            else
            {
                NPMediaServicesError *error = [NPMediaServicesError permissionNotAvailable];
                NSLog(@"Error capturing media content %@", error);
                
                if(callback != nil)
                {
                    callback(nil, error);
                }
            }
        }];
    }
    else
    {
        [[NSException exceptionWithName:NSInvalidArgumentException reason:@"This Capture type is not implemented." userInfo:@{NSLocalizedDescriptionKey: @"Capture type not implemented."}] raise];
    }
}

-(void) saveMediaContent:(NSData*) data withMimeType:(NSString*) mime withOptions:(MediaContentSaveOptions*) options withCallback:(SaveMediaContentCallback) callback
{
    NSString* fileNameWithExtension = [NSString stringWithFormat:@"%@.%@", options.fileName, [UTType typeWithMIMEType: mime].preferredFilenameExtension];
    NSURL *url = [NSURL fileURLWithPath:[NSTemporaryDirectory() stringByAppendingString:fileNameWithExtension]];
    [data writeToURL:url atomically:YES];
    
    
    if([self isImageMime:mime] || [self isVideoMime:mime])
    {
        //Use PHLibrary for adding this content (image/video)
        [self saveWithPhotoLibrary:url mime:mime options:options callback: callback];
    }
    else
    {
        [self saveWithDocumentPicker:url mime:mime options:options callback: callback];
    }
}

- (void)pickWithPhotoPicker:(NSString *)mimeType options:(MediaContentSelectionOptions *)options {
    PHPickerConfiguration *configuration = [[PHPickerConfiguration alloc] init];
    configuration.selectionLimit = options.maxAllowed;
    configuration.filter = [self isImageMime: mimeType] ? PHPickerFilter.imagesFilter : PHPickerFilter.videosFilter;
    
    PHPickerViewController *controller = [[PHPickerViewController alloc] initWithConfiguration: configuration];
    //controller.modalPresentationStyle = UIModalPresentationFullScreen;
    controller.presentationController.delegate = self;
    controller.delegate = self;
    
    [self presentPicker:controller];
}

- (void)pickWithDocumentPicker:(MediaContentSelectionOptions *)options {    
    UTType *allowedUTType = [UTType typeWithMIMEType: options.allowedMimeType];
    NSArray *contentTypes = (allowedUTType != NULL) ? @[allowedUTType] : @[UTTypeContent];
    UIDocumentPickerViewController *controller = [[UIDocumentPickerViewController alloc] initForOpeningContentTypes:contentTypes];
    controller.allowsMultipleSelection = options.maxAllowed > 1 || options.maxAllowed == 0; //No option to limit max elements that can be selected
    controller.delegate = self;
    
    [self presentPicker:controller];
}

-(void) presentPicker:(UIViewController*) controller
{
    [UnityGetGLViewController() presentViewController:controller animated:YES completion: ^{
        NPLog(@"Finished presenting picker controller");
    }];
}

-(void) saveWithPhotoLibrary:(NSURL*) url mime:(NSString*) mime options:(MediaContentSaveOptions*) options callback:(SaveMediaContentCallback) callback{
    
    PHAccessLevel accessLevel = (options != nil && options.directoryName != nil) ? PHAccessLevelReadWrite : PHAccessLevelAddOnly;
    [self requestPhotoLibraryPermission:accessLevel withCallback:^(BOOL isAuthorised, NPMediaServicesError *error) {
        if(!isAuthorised || error != nil)
        {
            if(callback != nil)
            {
                callback(FALSE, error);
            }
            return;
        }
        
        [[PHPhotoLibrary sharedPhotoLibrary] performChanges:^{
                    PHAssetChangeRequest    *createdAssetRequest         = [self createAssetChangeRequest:url isImage: [self isImageMime:mime]];
                    
                    if(options.directoryName != nil) //This may need additional permissions (like giving full access to photos as we need to fetch existing album before creating one.
                    {
                        [self addToAlbumCollection:createdAssetRequest albumName:options.directoryName];
                    }
                }
              completionHandler:^(BOOL success, NSError * _Nullable error) {
                    if(callback != nil)
                    {
                        callback(success, [NPMediaServicesError createFrom:error]);
                    }
            }
        ];
    }];
}

-(void) requestPhotoLibraryPermission:(PHAccessLevel) accessLevel withCallback:(void(^)(BOOL isAuthorised, NPMediaServicesError* error))callback
{
    [PHPhotoLibrary requestAuthorizationForAccessLevel:accessLevel handler:^(PHAuthorizationStatus status) {
        switch (status) {
            case PHAuthorizationStatusAuthorized:
                if(callback != nil)
                {
                    callback(TRUE, nil);
                }
                break;
            default:
                if(callback != nil)
                {
                    callback(FALSE, [NPMediaServicesError permissionNotAvailable]);
                }
        }
    }];
}

- (PHAssetChangeRequest *)createAssetChangeRequest:(NSURL *)url isImage:(BOOL) isImage {
    PHAssetChangeRequest *assetRequest = isImage ? [PHAssetChangeRequest creationRequestForAssetFromImageAtFileURL:url] : [PHAssetChangeRequest creationRequestForAssetFromVideoAtFileURL:url];
    return assetRequest;
}

- (void)addToAlbumCollection:(PHAssetChangeRequest *)createdAssetRequest albumName:(NSString *)albumName {
    PHAssetCollection               *existingAlbumCollection    = [self getAlbumCollectionIfExists: albumName];
    PHAssetCollectionChangeRequest  *collectionChangeRequest    = nil;
    
    if(existingAlbumCollection != nil)
    {
        collectionChangeRequest = [PHAssetCollectionChangeRequest changeRequestForAssetCollection: existingAlbumCollection];
    }
    else
    {
        collectionChangeRequest = [PHAssetCollectionChangeRequest creationRequestForAssetCollectionWithTitle:albumName];
    }
    
    [collectionChangeRequest addAssets:@[createdAssetRequest]];
}


- (PHAssetCollection*)getAlbumCollectionIfExists:(NSString*) albumName
{
    PHFetchOptions *fetchOptions = [[PHFetchOptions alloc] init];
    fetchOptions.predicate = [NSPredicate predicateWithFormat:@"title = %@", albumName];
    PHFetchResult<PHAssetCollection *> *fetchResult = [PHAssetCollection fetchAssetCollectionsWithType:PHAssetCollectionTypeAlbum
                                                           subtype:PHAssetCollectionSubtypeAny
                                                           options:fetchOptions];
    if(fetchResult.count > 0)
    {
        return [fetchResult objectAtIndex:0];
    }
    
    return nil;
}

-(void) saveWithDocumentPicker:(NSURL *)url mime:(NSString *)mime options:(MediaContentSaveOptions *)options callback: (SaveMediaContentCallback) callback{
    
    self.saveMediaContentCallback = callback;
    UIDocumentPickerViewController *controller = [[UIDocumentPickerViewController alloc] initForExportingURLs:@[url] asCopy:FALSE];
    controller.delegate = self;
    
}

-(BOOL) isImageMime:(NSString*) mime
{
    return [mime hasPrefix:@"image/"];
}

-(BOOL) isVideoMime:(NSString*) mime
{
    return [mime hasPrefix:@"video/"];
}

- (void)picker:(nonnull PHPickerViewController *)picker didFinishPicking:(nonnull NSArray<PHPickerResult *> *)results {
 
    NSLog(@"PHPickerViewController didFinishPicking: %ld %@", [results count], results);
    [picker dismissViewControllerAnimated:YES completion:nil];
    
    if(self.selectMediaContentCallback != nil)
    {
        
        if([results count] == 0)
        {
            self.selectMediaContentCallback(nil, [NPMediaServicesError userCancelled]);
        }
        else
        {
            NSMutableArray<NPMediaContent*> *mediaContents = [NSMutableArray array];
            for (PHPickerResult *eachResult in results) {
                [mediaContents addObject:[[NPItemProviderMediaContent alloc] initWithItemProvider: eachResult.itemProvider]];
            }
            
            self.selectMediaContentCallback(mediaContents, nil);
        }
        
        self.selectMediaContentCallback = nil;
    }
}

- (void)documentPicker:(UIDocumentPickerViewController *)controller didPickDocumentsAtURLs:(NSArray <NSURL *>*)urls
{
    if(self.selectMediaContentCallback != nil)
    {
        NSMutableArray<NPMediaContent*> *mediaContents = [NSMutableArray array];

        for (NSURL *eachUrl in urls) {
            [mediaContents addObject:[[NPContentUrlMediaContent alloc] initWithContentUrl: eachUrl]];
        }
        
        self.selectMediaContentCallback(mediaContents, nil);
        self.selectMediaContentCallback = nil;
    }
    else if(self.saveMediaContentCallback != nil)
    {
        self.saveMediaContentCallback(TRUE, nil);
    }
}

- (void)documentPickerWasCancelled:(UIDocumentPickerViewController *)controller
{
    if(self.selectMediaContentCallback != nil)
    {
        self.selectMediaContentCallback(nil, [NPMediaServicesError userCancelled]);
        self.selectMediaContentCallback = nil;
    }
    else if(self.saveMediaContentCallback != nil)
    {
        self.selectMediaContentCallback(nil, [NPMediaServicesError userCancelled]);
        self.saveMediaContentCallback = nil;
    }
}

#pragma mark - UIImagePickerControllerDelegate methods

- (void)imagePickerController:(UIImagePickerController *)picker didFinishPickingMediaWithInfo:(NSDictionary<UIImagePickerControllerInfoKey, id> *)info
{
    NPLog(@"Did finish picking media with info: %@", info);
    
    [picker dismissViewControllerAnimated:YES completion:nil];
    
    NSString *mediaType = [info objectForKey:UIImagePickerControllerMediaType];
    NPMediaContent *mediaContent = nil;
    
    if([mediaType isEqualToString:UTTypeMovie.identifier] || [mediaType isEqualToString:UTTypeVideo.identifier])
    {
        NSURL *mediaUrl = [info objectForKey: UIImagePickerControllerMediaURL];
        mediaContent = [[NPContentUrlMediaContent alloc] initWithContentUrl:mediaUrl];
    }
    else
    {
        UIImage*    editedImage     = [info objectForKey:UIImagePickerControllerEditedImage];
        UIImage*    originalImage   = [info objectForKey:UIImagePickerControllerOriginalImage];
        
        UIImage*    selectedImage   = editedImage ? editedImage : originalImage;
        NSData*     imageData       = NPEncodeImageAsData(selectedImage, UIImageEncodeTypePNG);
        
        mediaContent = [[NPRawDataMediaContent alloc] initWithContentData:imageData withMime:UTTypePNG.preferredMIMEType];
    }

    if(self.captureMediaContentCallback != nil)
    {
        self.captureMediaContentCallback(mediaContent, nil);
        self.captureMediaContentCallback = nil;
    }
}

- (void)imagePickerControllerDidCancel:(UIImagePickerController*)picker
{
    [picker dismissViewControllerAnimated:YES completion:nil];

    if(self.captureMediaContentCallback != nil)
    {
        self.captureMediaContentCallback(nil, [NPMediaServicesError userCancelled]);
        self.captureMediaContentCallback = nil;
    }
}

- (void)presentationControllerDidDismiss:(UIPresentationController *)presentationController API_AVAILABLE(ios(13.0))
{
    if(self.selectMediaContentCallback != nil)
    {
        self.selectMediaContentCallback(nil, [NPMediaServicesError userCancelled]);
        self.selectMediaContentCallback = nil;
        NSLog(@"presentationController %@", presentationController);
    }
    else
    {
        NSLog(@"Presentation handler dimiss not handled");
    }
}


@end
