// Client-side image compression for Snappers Repair Shop
// Compresses images to under 2MB before upload

window.imageCompression = {
    /**
     * Compress an image file to under 2MB
     * @param {File} file - The image file to compress
     * @param {number} maxSizeMB - Maximum size in MB (default: 2)
     * @param {number} maxWidthOrHeight - Maximum width or height (default: 1920)
     * @returns {Promise<Blob>} - Compressed image blob
     */
    compressImage: async function (file, maxSizeMB = 2, maxWidthOrHeight = 1920) {
        return new Promise((resolve, reject) => {
            const reader = new FileReader();
            
            reader.onload = function (e) {
                const img = new Image();
                
                img.onload = function () {
                    const canvas = document.createElement('canvas');
                    let width = img.width;
                    let height = img.height;
                    
                    // Calculate new dimensions while maintaining aspect ratio
                    if (width > height) {
                        if (width > maxWidthOrHeight) {
                            height = Math.round((height * maxWidthOrHeight) / width);
                            width = maxWidthOrHeight;
                        }
                    } else {
                        if (height > maxWidthOrHeight) {
                            width = Math.round((width * maxWidthOrHeight) / height);
                            height = maxWidthOrHeight;
                        }
                    }
                    
                    canvas.width = width;
                    canvas.height = height;
                    
                    const ctx = canvas.getContext('2d');
                    ctx.drawImage(img, 0, 0, width, height);
                    
                    // Start with quality 0.9 and reduce if needed
                    let quality = 0.9;
                    const maxSizeBytes = maxSizeMB * 1024 * 1024;
                    
                    const tryCompress = () => {
                        canvas.toBlob(
                            (blob) => {
                                if (blob.size <= maxSizeBytes || quality <= 0.5) {
                                    // Success or reached minimum quality
                                    resolve(blob);
                                } else {
                                    // Try again with lower quality
                                    quality -= 0.1;
                                    tryCompress();
                                }
                            },
                            'image/jpeg',
                            quality
                        );
                    };
                    
                    tryCompress();
                };
                
                img.onerror = function () {
                    reject(new Error('Failed to load image'));
                };
                
                img.src = e.target.result;
            };
            
            reader.onerror = function () {
                reject(new Error('Failed to read file'));
            };
            
            reader.readAsDataURL(file);
        });
    },

    /**
     * Get file size in MB
     * @param {Blob} blob - The blob to measure
     * @returns {number} - Size in MB
     */
    getFileSizeMB: function (blob) {
        return blob.size / (1024 * 1024);
    },

    /**
     * Convert blob to base64 for preview
     * @param {Blob} blob - The blob to convert
     * @returns {Promise<string>} - Base64 data URL
     */
    blobToBase64: async function (blob) {
        return new Promise((resolve, reject) => {
            const reader = new FileReader();
            reader.onloadend = () => resolve(reader.result);
            reader.onerror = reject;
            reader.readAsDataURL(blob);
        });
    }
};

// License plate camera capture
window.licensePlateCamera = {
    /**
     * Capture a photo from the device camera and return as base64
     * Uses a hidden file input with capture attribute for mobile compatibility
     * @param {string} inputId - The ID of the hidden file input element
     * @returns {Promise<string>} - Base64 image data (without the data:image prefix)
     */
    getBase64FromInput: async function (inputId) {
        const input = document.getElementById(inputId);
        if (!input || !input.files || input.files.length === 0) {
            return null;
        }

        const file = input.files[0];

        // Compress first to keep it reasonable for OCR
        const canvas = document.createElement('canvas');
        const img = new Image();

        return new Promise((resolve, reject) => {
            const reader = new FileReader();
            reader.onload = function (e) {
                img.onload = function () {
                    // Resize to max 1280px for OCR (don't need full res)
                    let width = img.width;
                    let height = img.height;
                    const maxDim = 1280;

                    if (width > height && width > maxDim) {
                        height = Math.round((height * maxDim) / width);
                        width = maxDim;
                    } else if (height > maxDim) {
                        width = Math.round((width * maxDim) / height);
                        height = maxDim;
                    }

                    canvas.width = width;
                    canvas.height = height;
                    canvas.getContext('2d').drawImage(img, 0, 0, width, height);

                    // Get base64 without the data:image/jpeg;base64, prefix
                    const dataUrl = canvas.toDataURL('image/jpeg', 0.85);
                    const base64 = dataUrl.split(',')[1];
                    resolve(base64);
                };
                img.onerror = () => reject(new Error('Failed to load image'));
                img.src = e.target.result;
            };
            reader.onerror = () => reject(new Error('Failed to read file'));
            reader.readAsDataURL(file);
        });
    }
};

