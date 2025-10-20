namespace App.Application.Contracts.Repositories.IUnitOfWork
{ }
public interface IUnitOfWork : IDisposable
{
    IGenericRepository<T> Repository<T>() where T : EntityBase;
    //IGenericRepository<Attachment> Attachment { get; }

    Task<int> Complete(string UserId = "", string IpAddress = "");

    //var products = await _unitOfWork.Repository<Product>().GetAsync(
    //    includeString: "Category"
    //);

    //var products = await _unitOfWork.Repository<Product>().GetAsync(
    //    predicate: p => p.Price >= minPrice && p.Price <= maxPrice,
    //    orderBy: q => q.OrderBy(p => p.Price)
    //);

    //var product = await _unitOfWork.Repository<Product>().GetAsync(
    //    predicate: p => p.Id == productId,
    //    includeString: "Category,Reviews,Supplier"
    //);

    //var products = await _unitOfWork.Repository<Product>().GetAsync(
    //    predicate: predicate,
    //    orderBy: q => q.OrderByDescending(p => p.CreatedAt),
    //    includeString: "Category",
    //    disableTracking: true
    //);
}
