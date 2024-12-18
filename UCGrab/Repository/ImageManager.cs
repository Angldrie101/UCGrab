using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using UCGrab.Database;
using UCGrab.Utils;

namespace UCGrab.Repository
{
    public class ImageManager
    {
        BaseRepository<Image> _img;
        BaseRepository<Image_Product> _imgproduct;
        BaseRepository<Image_Store> _imgstore;
        BaseRepository<File_Documents> _filedoc;

        public ImageManager()
        {
            _img = new BaseRepository<Image>();
            _imgproduct = new BaseRepository<Image_Product>();
            _imgstore = new BaseRepository<Image_Store>();
            _filedoc = new BaseRepository<File_Documents>();
        }

        public List<Image> ListImgAttachByImageId(int? id)
        {
            return _img._table.Where(m => m.image_id == id).ToList();
        }
        public ErrorCode CreateImg(Image img, ref String err)
        {
            return _img.Create(img, out err);
        }
        public ErrorCode UpdateImg(Image img, ref String err)
        {
            return _img.Update(img.id, img, out err);
        }
        public ErrorCode DeleteImg(int? id, ref String err)
        {
            return _img.Delete(id, out err);
        }

        public ErrorCode DeleteImgByProductId(int? id, ref String err)
        {
            foreach (var i in _img._table.Where(m => m.image_id == id).ToList())
            {
                DeleteImg(i.id, ref err);
            }
            return ErrorCode.Success;
        }

        public List<Image_Product> ListImgAttachByImageProductId(int? id)
        {
            return _imgproduct._table.Where(m => m.product_id == id).ToList();
        }
        public ErrorCode CreateImgProduct(Image_Product imgproduct, ref String err)
        {
            return _imgproduct.Create(imgproduct, out err);
        }
        public ErrorCode UpdateImgProduct(Image_Product imgproduct, ref String err)
        {
            return _imgproduct.Update(imgproduct.id, imgproduct, out err);
        }
        public ErrorCode DeleteImgProduct(int? id, ref String err)
        {
            return _imgproduct.Delete(id, out err);
        }

        public ErrorCode DeleteImgProductByProductId(int? id, ref String err)
        {
            foreach (var i in _imgproduct._table.Where(m => m.product_id == id).ToList())
            {
                DeleteImg(i.id, ref err);
            }
            return ErrorCode.Success;
        }

        public List<Image_Store> ListImgAttachByImageStoreId(int? id)
        {
            return _imgstore._table.Where(m => m.store_id == id).ToList();
        }
        public ErrorCode CreateImgStore(Image_Store imgstore, ref String err)
        {
            return _imgstore.Create(imgstore, out err);
        }
        public ErrorCode UpdateImgStore(Image_Store imgstore, ref String err)
        {
            return _imgstore.Update(imgstore.id, imgstore, out err);
        }
        public ErrorCode DeleteImgStore(int? id, ref String err)
        {
            return _imgstore.Delete(id, out err);
        }

        public ErrorCode DeleteImgProductByStoreId(int? id, ref String err)
        {
            foreach (var i in _imgstore._table.Where(m => m.store_id == id).ToList())
            {
                DeleteImg(i.id, ref err);
            }
            return ErrorCode.Success;
        }

        public string GetStoreQrCodeByStoreId(int? storeId)
        {
            var storeImage = _imgstore._table.FirstOrDefault(img => img.store_id == storeId);
            return storeImage != null ? storeImage.qr_file : "~/Assets/Shop/img/def.jpg"; // Default QR if none exists
        }

        public List<File_Documents> ListFileAttachByImageStoreId(int? id)
        {
            return _filedoc._table.Where(m => m.user_id == id).ToList();
        }
        public ErrorCode CreateFileDocument(File_Documents filedoc, ref String err)
        {
            return _filedoc.Create(filedoc, out err);
        }
        public ErrorCode UpdateFile(File_Documents filedoc, ref String err)
        {
            return _filedoc.Update(filedoc.id, filedoc, out err);
        }
        public ErrorCode DeleteFile(int? id, ref String err)
        {
            return _filedoc.Delete(id, out err);
        }

        public ErrorCode DeleteFilebyId(int? id, ref String err)
        {
            foreach (var i in _filedoc._table.Where(m => m.user_id == id).ToList())
            {
                DeleteImg(i.id, ref err);
            }
            return ErrorCode.Success;
        }
    }
}