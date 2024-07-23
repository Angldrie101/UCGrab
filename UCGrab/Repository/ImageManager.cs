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

        public ImageManager()
        {
            _img = new BaseRepository<Image>();
            _imgproduct = new BaseRepository<Image_Product>();
            _imgstore = new BaseRepository<Image_Store>();
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
    }
}