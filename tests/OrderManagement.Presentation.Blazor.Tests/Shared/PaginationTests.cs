using Bunit;

using Microsoft.AspNetCore.Components;

using OrderManagement.Presentation.Blazor.Components.Shared;

namespace OrderManagement.Presentation.Blazor.Tests.Shared
{
    [TestClass]
    public sealed class PaginationTests : BunitContext
    {
        private static readonly string[] ExpectedLeadingPages = ["1", "2", "3", "9"];

        [TestMethod]
        public void Render_FewPages_ShowsAllPagesWithoutEllipsis()
        {
            IRenderedComponent<Pagination> cut = Render<Pagination>(parameters => parameters
                .Add(p => p.CurrentPage, 1)
                .Add(p => p.TotalPages, 3));

            Assert.AreEqual(0, cut.FindAll(".pagination-ellipsis").Count);
            Assert.AreEqual(3, cut.FindAll(".pagination-page").Count);
        }

        [TestMethod]
        public void Render_ManyPagesAtFirstPage_ShowsLeadingPagesEllipsisAndLast()
        {
            IRenderedComponent<Pagination> cut = Render<Pagination>(parameters => parameters
                .Add(p => p.CurrentPage, 1)
                .Add(p => p.TotalPages, 9));

            string[] labels = [.. cut.FindAll(".pagination-page").Select(e => e.TextContent)];
            CollectionAssert.AreEqual(ExpectedLeadingPages, labels);
            Assert.AreEqual(1, cut.FindAll(".pagination-ellipsis").Count);
        }

        [TestMethod]
        public void Render_CurrentPage_HasAriaCurrent()
        {
            IRenderedComponent<Pagination> cut = Render<Pagination>(parameters => parameters
                .Add(p => p.CurrentPage, 2)
                .Add(p => p.TotalPages, 3));

            Assert.AreEqual("page", cut.FindAll(".pagination-page")[1].GetAttribute("aria-current"));
        }

        [TestMethod]
        public void Click_PageButton_InvokesCallback()
        {
            int? clickedPage = null;
            IRenderedComponent<Pagination> cut = Render<Pagination>(parameters => parameters
                .Add(p => p.CurrentPage, 1)
                .Add(p => p.TotalPages, 3)
                .Add(p => p.OnPageChange, EventCallback.Factory.Create<int>(this, page => clickedPage = page)));

            cut.FindAll(".pagination-page")[2].Click();

            Assert.AreEqual(3, clickedPage);
        }

        [TestMethod]
        public void Render_AtFirstPage_DisablesPreviousButton()
        {
            IRenderedComponent<Pagination> cut = Render<Pagination>(parameters => parameters
                .Add(p => p.CurrentPage, 1)
                .Add(p => p.TotalPages, 3));

            Assert.IsTrue(cut.Find(".pagination-prev").HasAttribute("disabled"));
            Assert.IsFalse(cut.Find(".pagination-next").HasAttribute("disabled"));
        }

        [TestMethod]
        public void Render_AtLastPage_DisablesNextButton()
        {
            IRenderedComponent<Pagination> cut = Render<Pagination>(parameters => parameters
                .Add(p => p.CurrentPage, 3)
                .Add(p => p.TotalPages, 3));

            Assert.IsTrue(cut.Find(".pagination-next").HasAttribute("disabled"));
            Assert.IsFalse(cut.Find(".pagination-prev").HasAttribute("disabled"));
        }
    }
}
