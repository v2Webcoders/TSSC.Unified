/**
 * @license Copyright (c) 2003-2015, CKSource - Frederico Knabben. All rights reserved.
 * For licensing, see LICENSE.md or http://ckeditor.com/license
 */

//CKEDITOR.editorConfig = function( config ) {
//	// Define changes to default configuration here. For example:
//	// config.language = 'fr';
//	// config.uiColor = '#AADC6E';
//};
CKEDITOR.editorConfig = function (config) {
    config.toolbar = 'MyToolbar';
    config.allowedContent = true;
    config.protectedSource.push(/<i[^>]*><\/i>/g);

    config.toolbar_MyToolbar =
	[
        { name: 'document', items: ['Source', '-',  'NewPage', 'Preview', 'Print'] },
		//{ name: 'styles', items: ['Font', 'FontSize', 'TextColor'] },
        { name: 'styles', items: ['Font', 'FontSize', 'TextColor'] },
		{ name: 'basicstyles', items: ['Bold', 'Italic', 'Underline'] },
		{ name: 'paragraph', items: ['NumberedList', 'BulletedList', 'Table', '-', 'Outdent', 'Indent', '-', 'JustifyLeft', 'JustifyCenter', 'JustifyRight', 'JustifyBlock'] },
		{ name: 'links', items: ['Link', 'Unlink'] },
	    { name: 'clipboard', items: ['Cut', 'Copy', 'Paste', '-', 'Undo', 'Redo'] },
	];
    config.toolbar = 'MyToolbar';

    config.toolbar_MyToolbar_Minimal =
	[
		//{ name: 'styles', items: ['Font', 'FontSize', 'TextColor'] },
		{ name: 'basicstyles', items: ['Bold', 'Italic', 'Underline'] },
		
		
	];
};